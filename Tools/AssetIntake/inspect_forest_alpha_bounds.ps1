param([Parameter(Mandatory=$true)][string]$ZipPath,[Parameter(Mandatory=$true)][string]$OutputPath)
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using System.Runtime.InteropServices;
public static class ForestCardAlphaBounds {
 public static float[] Read(byte[] png){using(var stream=new MemoryStream(png))using(var image=new Bitmap(stream)){
  var data=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
  try{byte[] row=new byte[Math.Abs(data.Stride)];int left=image.Width,top=image.Height,right=-1,bottom=-1;
   for(int y=0;y<image.Height;y++){Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);for(int x=0;x<image.Width;x++)if(row[x*4+3]>8){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}}
   if(right<left)throw new Exception("Empty alpha card");left=Math.Max(0,left-2);right=Math.Min(image.Width-1,right+2);top=Math.Max(0,top-2);bottom=Math.Min(image.Height-1,bottom+2);
   return new float[]{(float)left/image.Width,1f-(float)(bottom+1)/image.Height,(float)(right+1)/image.Width,1f-(float)top/image.Height};
  }finally{image.UnlockBits(data);}
 }}
}
'@
$archive=[IO.Compression.ZipFile]::OpenRead($ZipPath)
try{
 $rows=@()
 foreach($entry in $archive.Entries){if($entry.FullName.Contains('/PNG_512/') -and $entry.FullName.EndsWith('.png')){
  $stream=$entry.Open();$memory=[IO.MemoryStream]::new()
  try{$stream.CopyTo($memory);$rows+=[pscustomobject]@{file=$entry.Name;alpha_uv_bounds=[ForestCardAlphaBounds]::Read($memory.ToArray())}}finally{$stream.Dispose();$memory.Dispose()}
 }}
 $json=ConvertTo-Json -InputObject $rows -Depth 4
 [IO.File]::WriteAllText($OutputPath,$json,[Text.UTF8Encoding]::new($false))
}finally{$archive.Dispose()}
