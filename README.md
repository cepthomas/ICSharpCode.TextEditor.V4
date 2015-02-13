ICSharpCode.TextEditor.V4
=========================

An evolution of the old WinForm text editor. After evaluating a lot of editors for my text/code editor project, I stuck with this old favorite. However it has a few deficiencies which I intend to correct.

- Dynamic loading of style/format files. Support user defined and editing.
- Rectangle region cut/copy/paste.
- Line wrap, or hover displays the whole line (like folded sections in VS).
- Add support for Lua.
- Maybe some performance tweaks.

The baseline is from ICSharpCode V3.2.1.7.

Hiatus
===============
After wading around in a lot of the ICSharpCode.TextEditor.V4 I've decided that the better course is to
build an editor around AvalonEdit. So I've forked that and started a new version.
This can stay here for a while.
