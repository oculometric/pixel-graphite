# pre v0.6
- [x] add free-look editing mode
- [x] rewrite voxel storage
- [x] rewrite voxel generation to be more efficient
- [x] optimise mesh indexing
- [x] split mesh generation into blocks to reduce indexing cost and eliminate need to rebuild entire mesh (possibly store file as blocks too?)
- [x] mesh exporting
- [x] warning on load/exit
- [x] smooth ghost movement
- [x] lock mouse during pan
- [x] message display/dialog for errors and infos
- [x] lock free look rotation
- [x] fix F control overlap
- [x] fix screen wrapping shader issue
- [x] fix blocks misplacing
- [x] custom loading screen
- [x] rewrite legacy loader
- [x] implement RLE for dat files to reduce size (or use zip?)
- [x] implement autosave and improved saving/loading featues in general (save vs save as, tracking the currently edited file)

# v0.6
- [x] rendering parameters config at runtime (pixelation, posterise, palette, backgrounf control/filtering)
- [x] background filtering control (add to shader and rendering params)
- [x] add help page and move controls there
- [x] add ability to save and load rendering palettes (3x1 pixel EXR file???)
- [x] fix stretched screenshots
- [x] ability to reset shader to default
- [x] ability to toggle 3-color mode
- [x] fix fonts in popups
- [ ] add icons for new voxel types
- [ ] improve raytracing in voxel mode
- [ ] reimplement displacement in shader
- [ ] implement saving of lighting data, rendering config, ivy data, sand, etc (new file format with multiple sub-headers/datablocks within, including legacy support)

# v0.7
- [ ] ivy growth engine/editor mode


- [ ] add main menu
- [ ] custom cursor
- [ ] sand editing/toggle
- [ ] add camera perspective controls?
- [ ] lily pad painting