import json
from pathlib import Path
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[1]/"art/heroes/Approved/v2"
directions=["east","north-east","north","north-west","west","south-west","south","south-east"]
rows=[]
for hero in ("amelia","sam","zike"):
    export=root/f"{hero}_pixellab"
    data=json.loads((export/"metadata.json").read_text(encoding="utf-8"))
    state=data["states"][0]
    row=Image.new("RGBA",(96*8,96),(0,0,0,0)); report=[]
    for col,direction in enumerate(directions):
        image=Image.open(export/state["frames"]["rotations"][direction]).convert("RGBA")
        row.alpha_composite(image,(col*96,0));box=image.getchannel("A").getbbox()
        report.append({"direction":direction,"bounds":box,"width":0 if not box else box[2]-box[0],"height":0 if not box else box[3]-box[1],"feet_y":None if not box else box[3]})
    row.save(root/f"{hero}_rotations_8dir.png")
    rows.append(row)
    (root/f"{hero}_rotation_audit.json").write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")

sheet=Image.new("RGBA",(96*8,96*3),(18,20,24,255))
for y,row in enumerate(rows):sheet.alpha_composite(row,(0,y*96))
sheet.resize((96*8*2,96*3*2),Image.Resampling.NEAREST).save(root/"approved_v2_rotations_comparison_2x.png")
