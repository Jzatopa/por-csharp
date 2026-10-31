# Nuked-OPL3

Unmodified opl3.c, opl3.h and LICENSE from https://github.com/nukeykt/Nuked-OPL3,
revision 765ec962e473aeb767e4cba74ffdc8f588ffbfe8 (LGPL-2.1-or-later; see source notices).
Copyright and author notices are preserved in the files.

The desktop dynamically loads libpor_opl.so. It can be replaced independently.
Build/rebuild: `cc -O2 -fPIC -shared native/por_opl.c third_party/nuked-opl3/opl3.c -o libpor_opl.so`.
The desktop project builds and copies this library on Linux; sources and license must
accompany distributions. No original game data is included here.
