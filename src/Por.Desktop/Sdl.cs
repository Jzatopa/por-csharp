using System.Runtime.InteropServices;
namespace Por.Desktop;
internal static class Sdl
{
 const string Lib="libSDL2-2.0.so.0";
 [DllImport(Lib)] public static extern int SDL_Init(uint flags);
 [DllImport(Lib)] public static extern int SDL_InitSubSystem(uint flags);
 [StructLayout(LayoutKind.Sequential)] public struct AudioSpec {public int Frequency;public ushort Format;public byte Channels,Silence;public ushort Samples,Padding;public uint Size;public IntPtr Callback,UserData;}
 [DllImport(Lib)] public static extern uint SDL_OpenAudioDevice(IntPtr device,int capture,ref AudioSpec desired,out AudioSpec obtained,int changes);
 [DllImport(Lib)] public static extern void SDL_PauseAudioDevice(uint device,int pause);
 [DllImport(Lib)] public static extern int SDL_QueueAudio(uint device,short[] samples,uint bytes);
 [DllImport(Lib)] public static extern uint SDL_GetQueuedAudioSize(uint device);
 [DllImport(Lib)] public static extern void SDL_ClearQueuedAudio(uint device);
 [DllImport(Lib)] public static extern void SDL_CloseAudioDevice(uint device);
 [DllImport(Lib)] public static extern IntPtr SDL_GetCurrentAudioDriver();
 public static string AudioDriver=>Marshal.PtrToStringUTF8(SDL_GetCurrentAudioDriver());
 [DllImport(Lib)] public static extern IntPtr SDL_CreateWindow([MarshalAs(UnmanagedType.LPUTF8Str)] string title,int x,int y,int w,int h,uint flags);
 [DllImport(Lib)] public static extern IntPtr SDL_CreateRenderer(IntPtr window,int index,uint flags);
 [DllImport(Lib)] public static extern IntPtr SDL_CreateTexture(IntPtr renderer,uint format,int access,int w,int h);
 [DllImport(Lib)] public static extern int SDL_UpdateTexture(IntPtr texture,IntPtr rect,uint[] pixels,int pitch);
 [DllImport(Lib)] public static extern int SDL_RenderClear(IntPtr renderer);
 [DllImport(Lib)] public static extern int SDL_RenderCopy(IntPtr renderer,IntPtr texture,IntPtr src,IntPtr dst);
 [DllImport(Lib)] public static extern void SDL_RenderPresent(IntPtr renderer);
 [DllImport(Lib)] public static extern int SDL_PollEvent(out Event e);
 [DllImport(Lib)] public static extern void SDL_DestroyTexture(IntPtr p);
 [DllImport(Lib)] public static extern void SDL_DestroyRenderer(IntPtr p);
 [DllImport(Lib)] public static extern void SDL_DestroyWindow(IntPtr p);
 [DllImport(Lib)] public static extern void SDL_Quit();
 [DllImport(Lib)] private static extern IntPtr SDL_GetError();
 public static string Error=>Marshal.PtrToStringUTF8(SDL_GetError());
 // SDL_Event is56bytes on64bit; SDL_KeyboardEvent keysym starts at16, sym at20, mod at24.
 [StructLayout(LayoutKind.Explicit,Size=56)]public struct Event{[FieldOffset(0)]public uint Type;[FieldOffset(13)]public byte Repeat;[FieldOffset(20)]public int Key;[FieldOffset(24)]public ushort Mod;}
}
