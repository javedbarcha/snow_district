Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;
public static class SheetExtract {
 public static void Run(string source,string output){
 string[] names={"camera","garage","pause","lights-off","lights-on","steer-left","steer-right","blade-left","blade-straight","blade-right","dump","lift-toggle","forward","reverse"};
 int[,] boxes={{30,35,285,330},{325,35,305,320},{670,65,220,270},{945,100,260,225},{1225,80,310,245},{1570,40,270,320},{1865,35,275,325},{35,390,280,300},{335,395,280,270},{635,390,265,305},{910,380,310,300},{1240,400,290,275},{1560,380,265,300},{1860,395,265,285}};
 using(var src=new Bitmap(source)){for(int i=0;i<names.Length;i++){
 int bx=boxes[i,0],by=boxes[i,1],w=boxes[i,2],h=boxes[i,3],minx=w,miny=h,maxx=0,maxy=0;
 for(int y=0;y<h;y++)for(int x=0;x<w;x++){if(src.GetPixel(bx+x,by+y).A>32){minx=Math.Min(minx,x);miny=Math.Min(miny,y);maxx=Math.Max(maxx,x);maxy=Math.Max(maxy,y);}}
 minx=Math.Max(0,minx-4);miny=Math.Max(0,miny-4);maxx=Math.Min(w-1,maxx+4);maxy=Math.Min(h-1,maxy+4);
 using(var dst=new Bitmap(maxx-minx+1,maxy-miny+1,PixelFormat.Format32bppArgb)){for(int y=miny;y<=maxy;y++)for(int x=minx;x<=maxx;x++){var c=src.GetPixel(bx+x,by+y);if(c.A<=16)c=Color.FromArgb(0,c.R,c.G,c.B);dst.SetPixel(x-minx,y-miny,c);}dst.Save(Path.Combine(output,names[i]+".png"),ImageFormat.Png);Console.WriteLine(names[i]+" "+dst.Width+"x"+dst.Height);}
 }}}
}
"@
[SheetExtract]::Run((Join-Path $pwd 'SourceAssets/ControlIconSheet/Original.png'),(Join-Path $pwd 'Assets/Snowglow/Catalog/Sources/ControlIconSheet'))
