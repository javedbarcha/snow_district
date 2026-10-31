import sys,json,re,hashlib
from pathlib import Path
sys.path.insert(0,str(Path('Logs/PSDTools').resolve()))
from psd_tools import PSDImage
from PIL import Image,ImageDraw,ImageFont
source=Path('SourceAssets/SnowDistrictUI/Snow_District_UI_Buttons.psd')
psd=PSDImage.open(source)
out=Path('Assets/Snowglow/Catalog/Sources/SnowDistrictUI');out.mkdir(parents=True,exist_ok=True)
log=Path('Logs/SnowDistrictUI');log.mkdir(exist_ok=True)
psd.composite().save(log/'PSD-overview.png')
records=[];atlas=Image.new('RGBA',(2048,1024));sheet=Image.new('RGB',(1280,800),'#e4edf0');draw=ImageDraw.Draw(sheet)
for layer in psd.descendants():
 if layer.is_group():continue
 if not re.match(r'^\d\d \| ',layer.name) and not layer.name.startswith('Play | '):continue
 slug=re.sub('[^a-z0-9]+','-',layer.name.lower()).strip('-')
 image=layer.topil().convert('RGBA');bbox=image.getbbox();assert bbox,layer.name
 image=image.crop(bbox)
 image.save(out/(slug+'.png'))
 i=len(records);x=i%8*256;y=i//8*192
 atlas.alpha_composite(image,(x+(256-image.width)//2,y+(192-image.height)//2))
 preview=image.copy();preview.thumbnail((150,115));sx=i%8*160;sy=i//8*160
 sheet.paste(preview,(sx+(160-preview.width)//2,sy),preview);draw.text((sx+6,sy+120),layer.name,fill='#214750')
 records.append(dict(name=layer.name,slug=slug,width=image.width,height=image.height,x=x+(256-image.width)//2,y=1024-(y+(192-image.height)//2)-image.height,sourceBounds=list(layer.bbox),alpha=True))
atlas.save(out/'ButtonsAtlas.png');sheet.save(log/'Buttons-contact-sheet.png')
(out/'ExportManifest.json').write_text(json.dumps(dict(source=str(source),sha256=hashlib.sha256(source.read_bytes()).hexdigest(),width=psd.width,height=psd.height,atlasWidth=2048,atlasHeight=1024,buttons=records),indent=2))
print('Exported',len(records),'transparent button/state layers; atlas',atlas.size)
print([(r['name'],r['width'],r['height']) for r in records])
