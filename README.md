# vsix-escape-codeblock

A Visual Studio Extension for escaping codeblock

## Function

Move your caret to the closing bracket "}" of codeblock which the caret is currently located.

## Logic

Starting from the position of the current caret position, read character by character. If it find a open bracket "{", it expect to see a close bracket "}" to pair up the open bracket. Each pair of bracket it find indicate a inner nested codeblock, which is NOT our target. Using a counter, we count open bracket as +1, and close bracket as -1.

Continue the search until it find a sole closing bracket without a open bracket before, which means the counter is changing from 0 to -1, indicate the close bracket "should" pair with the current codeblock open bracket (which is NOT in the searching before, hence not in the counter).

After finding the target close bracket, move caret to position after the bracket.

## Limitation

* Currently only works for "{" as open bracket, and "}" as close bracket. (Hard-coded, you may change it and compile the vsix yourself)
* If your codeblock structure is not correct in the first place (e.g. open and close bracket does not fully pair up), the result may not be expected. The logic of the program rely solely on counting brackets, NOT understanding the code structure.
