using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;

namespace vsix_escape_codeblock
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class escape_codeblock
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("79f01bd2-9f70-4c5b-b4d0-cc1a65f16590");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly AsyncPackage package;

		/// <summary>
		/// Initializes a new instance of the <see cref="escape_codeblock"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private escape_codeblock(AsyncPackage package, OleMenuCommandService commandService)
		{
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new MenuCommand(this.Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static escape_codeblock Instance { get; private set; }

		/// <summary>
		/// Gets the service provider from the owner package.
		/// </summary>
		private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
		{
			get { return this.package; }
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static async Task InitializeAsync(AsyncPackage package)
		{
			// Switch to the main thread - the call to AddCommand in escape_codeblock's constructor requires
			// the UI thread.
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
			Instance = new escape_codeblock(package, commandService);
		}

		/// <summary>
		/// This function is the callback used to execute the command when the menu item is clicked.
		/// See the constructor to see how the menu item is associated with this function using
		/// OleMenuCommandService service and MenuCommand class.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void Execute(object sender, EventArgs e)
		{
			//Get TextManager service
			var textManager = ServiceProvider.GetServiceAsync(typeof(SVsTextManager)).Result as IVsTextManager2;

			//Get Text Editor View
			if (textManager.GetActiveView2(1, null, (uint) _VIEWFRAMETYPE.vftCodeWindow, out var view) != Microsoft.VisualStudio.VSConstants.S_OK) return;

			//Get current caret position
			view.GetCaretPos(out var currentLine, out var currentColumn);

			//Get buffer from view
			if (view.GetBuffer(out var textBuffer) != Microsoft.VisualStudio.VSConstants.S_OK) return;

			//Get index of last line
			if (textBuffer.GetLastLineIndex(out var lastLine, out var lastLineIndexPointer) != Microsoft.VisualStudio.VSConstants.S_OK) return;

			//Get line length of last line
			if (textBuffer.GetLengthOfLine(lastLine, out var lastLineLength) != Microsoft.VisualStudio.VSConstants.S_OK) return;

			var bracketStackCount = 0;
			var targetLine = 0;
			var targetColumn = 0;

			//Loop each line, starting from current line
			for (int lineIndex = currentLine; lineIndex < lastLine + 1; lineIndex++)
			{
				//Get line start
				var lineStartColumn = 0;
				if (lineIndex == currentLine) { lineStartColumn = currentColumn; }

				//Get line length(line end)
				if (textBuffer.GetLengthOfLine(lineIndex, out var lineLength) != Microsoft.VisualStudio.VSConstants.S_OK) return;

				//Get lineContent
				string lineContent;
				bool isFound = false;

				//Current caret line, checking from caret position
				if (textBuffer.GetLineText(lineIndex, lineStartColumn, lineIndex, lineLength, out lineContent) != Microsoft.VisualStudio.VSConstants.S_OK) return;

				//Loop each character
				for (int columnIndex = 0; columnIndex < lineLength - lineStartColumn; columnIndex++)
				{
					var c = lineContent[columnIndex];
					switch (c)
					{
						case '{':

							//Found open bracket, expect a closing bracket to cancel each other
							bracketStackCount++;
							break;
						case '}':

							//Found closing bracket, if there is no more open bracket before to cancel, target is found
							bracketStackCount--;
							if (bracketStackCount == -1) { isFound = true; }
							break;
					}
					if (isFound)
					{
						targetLine = lineIndex;
						targetColumn = columnIndex + lineStartColumn + 1;
						break;
					}
				}

				if (isFound)
				{
					view.SetCaretPos(targetLine, targetColumn);
					break;
				}
			}
		}
	}
}