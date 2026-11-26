using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
namespace ITDigitacao.Legacy.Shortcut
{
 internal static class Program
 {
  [STAThread] private static void Main()
  {
   string folder=AppDomain.CurrentDomain.BaseDirectory, exe=Path.Combine(folder,"IT-Digitacao.exe");
   if(!File.Exists(exe)){MessageBox.Show("IT-Digitacao.exe não foi encontrado. Extraia o pacote completo.","Criar atalho",MessageBoxButton.OK,MessageBoxImage.Error);return;}
   try {
    DriveInfo drive=new DriveInfo(Path.GetPathRoot(folder));
    if(drive.DriveType==DriveType.Removable && MessageBox.Show("O atalho deixará de funcionar quando o dispositivo for removido. Copie primeiro a pasta completa para o computador. Criar mesmo assim?","Unidade removível",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
    string desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), link=Path.Combine(desktop,"IT Digitação.lnk");
    Type shellType=Type.GetTypeFromProgID("WScript.Shell"); if(shellType==null)throw new InvalidOperationException("WScript.Shell indisponível.");
    object shell=Activator.CreateInstance(shellType), shortcut=shellType.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{link}); Type type=shortcut.GetType();
    type.InvokeMember("TargetPath",BindingFlags.SetProperty,null,shortcut,new object[]{exe}); type.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,shortcut,new object[]{folder});
    string icon=Path.Combine(folder,"Assets\\it-digitacao.ico"); type.InvokeMember("IconLocation",BindingFlags.SetProperty,null,shortcut,new object[]{(File.Exists(icon)?icon:exe)+",0"}); type.InvokeMember("Save",BindingFlags.InvokeMethod,null,shortcut,null);
    Marshal.FinalReleaseComObject(shortcut); Marshal.FinalReleaseComObject(shell); MessageBox.Show("Atalho criado na área de trabalho.","Criar atalho",MessageBoxButton.OK,MessageBoxImage.Information);
   } catch(Exception ex){MessageBox.Show("Não foi possível criar o atalho.\n"+ex.Message,"Criar atalho",MessageBoxButton.OK,MessageBoxImage.Error);}
  }
 }
}
