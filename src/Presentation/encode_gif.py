import json
import sys
from pathlib import Path
from PIL import Image

root = Path(sys.argv[1])
meta = json.loads((root / 'render.json').read_text())
files = [root / 'Frames' / f'{i:04}.png' for i in range(meta['frames'])]
# A shared palette prevents color flicker between demonstration steps.
samples = [Image.open(files[int(i * (len(files) - 1) / 13)]).convert('RGB') for i in range(14)]
sheet = Image.new('RGB', (meta['width'], meta['height'] * len(samples)))
for index, sample in enumerate(samples):
    sheet.paste(sample, (0, meta['height'] * index))
palette = sheet.quantize(colors=192, method=Image.Quantize.MEDIANCUT)
frames = []
for file in files:
    with Image.open(file) as frame:
        frames.append(frame.convert('RGB').quantize(palette=palette, dither=Image.Dither.NONE))
durations = [round((i+1)*100/meta['fps'])*10-round(i*100/meta['fps'])*10 for i in range(len(frames))]
output = root / 'pipe-wrench-showcase-snappy.gif'
frames[0].save(output, save_all=True, append_images=frames[1:], duration=durations, loop=0, optimize=True, disposal=1)
assert output.stat().st_size < 8_000_000, 'GIF exceeds the requested 8 MB limit'
with Image.open(output) as gif:
    duration = sum(frame.info.get('duration', 0) for frame in __import__('PIL.ImageSequence', fromlist=['Iterator']).Iterator(gif))
    result = dict(file=str(output), bytes=output.stat().st_size, megabytes=round(output.stat().st_size / 1_000_000, 3), width=gif.width, height=gif.height, frames=gif.n_frames, duration_ms=duration, loops=gif.info.get('loop'))
(root / 'verification.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result))
