# ICSharpCode.TextEditor.V4
An evolution of the old WinForm text editor. After evaluating a lot of editors for my text/code editor project, I stuck with this old favorite. However it has a few deficiencies which I intend to correct.

- Dynamic loading of style/format files. Support user defined and editing.
- Rectangle region cut/copy/paste.
- Line wrap, or hover displays the whole line (like folded sections in VS).
- Add support for Lua.
- Maybe some performance tweaks.

The baseline is from ICSharpCode V3.2.1.7.

# Hiatus
After wading around in a lot of the ICSharpCode.TextEditor.V4 I've decided that the better course is to
build an editor around AvalonEdit. So I've forked that and started a new version.
This can stay here for a while.

# Reboot
Another whack at this to go along with a slowly evolving editor project.

Things that have changed from the original (so far):
- Rectangle selection mostly works.
- Modernized HighlightingManager.
- Added printer support.
- Added finder class. Not fully functional yet and needs unit tests.
- Minimal Markdown syntax and folding. Good enough for what I want to do.
- Folding is now specified in the syntax file and loaded dynamically.
- Added Lua syntax.
- Dynamically load syntax definitions from file.
- Removed external dependencies so it can build.
- Upped to .NET 4.7.2, VS2019.
