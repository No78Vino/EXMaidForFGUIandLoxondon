# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2023-10-09

自己用的时候发现了HoudiniEngine存在Valid Session但实际已经失效的情况。简单翻了一下HoudiniEngine源码，不知道怎么检测Session的真实
连接情况，所以加了HEngine Session Sync的按钮接口。自行Disconnect,然后自行再connect houdini。
       
### Added

- v1.1 追加了HEngine Session Sync的按钮接口，直接弹出会话同步的管理窗口.

### Fixed

- none

### Changed

- none

### Removed

- none