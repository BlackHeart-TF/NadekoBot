# Changelog

Experimental changelog. Mostly based on [keepachangelog](https://keepachangelog.com/en/1.0.0/) except date format. 

## [2.45] 02.08.2024

### Changed

- .net8 and discord.net 3.15
- youtube related videos wont work, commented out line. most things untested, but it does in fact respond to commands again

## [2.44.4] - 06.06.2021

### Added

- Re-added `%music.playing%` and `%music.queued%` (#290)
- Added `%music.servers%` which shows how many servers have a song queued up to play  
ℹ️ ^ Only available to `.ropl` / `.adpl` feature atm
- `.autodc` re-added
- `.qrp`, `.vol`, `.smch` `.autodc` will now persist

### Changed

- Using `.commands` / `.cmds` without a module will now list modules
- `.qrp` / `.queuerepeat` will now accept one of 3 values
    - `none` - don't repeat queue
    - `track` - repeat single track
    - `queue` (or ommit) - repeat entire queue
- your old `.defvol` and `.smch` settings will be reset

### Fixed

- Fixed `.google` / `.g` command
- Removing last song in the queue will no longer reset queue index
- Having `.rpl` disabled will now correctly stop after the last song, closes #292

### Removed 

- `.sad` removed. It's more or less useless. Use `.qrp` and `.autodc` now for similar effect

### Obsolete

- `.rcs` is obsolete, use `.qrp s` or `.qrp song`
- `.defvol` is obsolete, use `.vol`

## [2.44.3] - 04.06.2021

### Changed

- Minor perf improvement for filter checks

### Fixed 

- `.qs` result urls are now valid
- Custom reactions with "`-`" as a response should once again disable that custom reaction completely
- Fixed `.acrm` out of range string
- Fixed `.sclist` and `.aclist` not showing correct indexes past page 1

## [2.44.2] - 02.06.2021

### Added

- Music related commands reimplemented with custom code, **considered alpha state**
- Song and playlist caching (faster song queue after first time)
- Much faster starting and skipping once the songs are in the queue
- Higher quality audio (no stuttering too!)
- Local tracks will now have durations if you have ffprobe installed (comes with ffmpeg)
- Bot supports joining a different vc without skipping the song if you use `.j` 
  - ⚠️ **DO NOT DRAG THE BOT** to another vc, as it's not properly supported atm, and you will have to do `.play` after dragging it) 
- `.j` makes the bot join your voice channel
- `.p` is now alias of play, pause is `.pause`
- `.qs` should work without google api key now for most users as it is using a custom loader
- Added `.clubs` alias for `.clublb`

### Changed

- `.ms` no longer takes `>` between arguments (`.ms 1 5` now, was `.ms 1>5` before)
- FlowerShop renamed to Shop

### Fixed

- Fixed decay bug giving everyone 1 flower every 24h
- Fixed feeds which have rss media items without a type
- Fixed `.acrm` index not working
- Fixed and error reply when a waifu item doesn't exist
- Disabled colored console on windows as they were causing issues for some users
- Fixed/Updated some strings and several minor bugfixes

### Removed

- Removed admin requirement on `.scrm` as it didn't make sense
- Some Music commands are removed because of the complexity they bring in with little value (if you *really* want them back, you can open an issue and specify your *good* reason)