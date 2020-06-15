# Changelog
All updates to the Ryujinx official master build will be documented in this file

## 1.0.4697 - 2020-06-14
### Fixed
- Fixed an issue where part of the VABS instruction would be parsed as an input register. 
  - Fixes a particular DEADLY PREMONITION Origins missing opcode error; the instruction was not missing but was instead parsed incorrectly. 

## 1.0.4696 - 2020-06-14
### Changed
- LayoutConverter has separate optimizations for LinearStrided and BlockLinear. MethodCopyBuffer now determines the range that will be affected, and uses a faster per pixel copy and offset calculation. 
  - This should increase performance on Nintendo Switch Online: NES and Super NES games, as well as mitigate dropped frames during large black screen (nvdec) videos.

## 1.0.4687 - 2020-06-09
### Changed
- Console logging now discards data in an overflow condition.
  - This can reduce cases where the game deadlocks or crashes because the console is in Select mode or is manually scrolled.

## 1.0.4683 - 2020-06-06
### Changed
- Stubbed ssl ISslContext: 4 (ImportServerPki) service
  - Fixes missing service crashes on Minecraft Dungeons and Rocket League

## 1.0.4682 - 2020-06-05
### Added
- Add Pclmulqdq intrinsic
  - Implemented crc32 in terms of pclmulqdq
  
## 1.0.4675 - 2020-06-02
### Fixed
- Fixed some SurfaceFlinger bugs
  - Brings this implementation closer to the real implementation.
  
