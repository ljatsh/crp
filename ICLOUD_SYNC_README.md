# iCloud 同步脚本使用说明

## 脚本功能

`sync_to_icloud.sh` 用于将 Unity Shader Library 文档自动同步到 iCloud Drive，方便在 iPhone Obsidian 中访问。

## 使用方法

### 第一次使用

1. **直接运行脚本**：
   ```bash
   ./sync_to_icloud.sh
   ```

2. **或者使用完整路径**：
   ```bash
   /Users/longjunwang/dev/GitHub/crp/sync_to_icloud.sh
   ```

### 后续使用

每次更新文档后，只需再次运行脚本即可同步：

```bash
cd /Users/longjunwang/dev/GitHub/crp
./sync_to_icloud.sh
```

## 脚本特性

- ✅ **自动检查**：验证源目录和 iCloud Drive 是否存在
- ✅ **智能同步**：使用 `rsync` 只同步更改的文件
- ✅ **排除无用文件**：自动排除 `.DS_Store`、临时文件等
- ✅ **保留时间戳**：保持文件的创建和修改时间
- ✅ **删除清理**：自动删除目标目录中已不存在的文件（`--delete`）
- ✅ **进度显示**：显示同步进度和文件统计

## 同步位置

- **源目录**：`~/dev/GitHub/crp/docs/ShaderLibrary`
- **目标目录**：`~/Library/Mobile Documents/com~apple~CloudDocs/Obsidian/ShaderLibrary`

## 在 iPhone Obsidian 中打开

1. 打开 iPhone 上的 Obsidian App
2. 点击左下角的 **设置**（齿轮图标）
3. 选择 **文件与链接**
4. 点击 **打开其他库**
5. 选择 **iCloud Drive**
6. 导航到 **Obsidian → ShaderLibrary**
7. 选择该文件夹作为 Obsidian 库

## 注意事项

⚠️ **首次同步**：
- 文件较多时可能需要几分钟时间
- 确保网络连接稳定
- iCloud 同步可能需要一些时间才能显示在 iPhone 上

⚠️ **后续同步**：
- 脚本使用 `--delete` 选项，会删除目标目录中源目录已不存在的文件
- 如果只想添加/更新文件而不删除，可以注释掉脚本中的 `--delete` 选项

⚠️ **iCloud 空间**：
- 确保有足够的 iCloud 存储空间
- 文档文件较小，通常不会占用太多空间

## 自定义配置

如果需要修改同步路径，可以编辑脚本中的以下变量：

```bash
SOURCE_DIR="$HOME/dev/GitHub/crp/docs/ShaderLibrary"
TARGET_DIR="$ICLOUD_BASE/Obsidian/ShaderLibrary"
```

## 故障排查

### 问题：提示 "iCloud Drive 未找到"

**解决方案**：
1. 检查系统偏好设置 → Apple ID → iCloud
2. 确保已启用 "iCloud Drive"
3. 等待 iCloud Drive 同步完成

### 问题：文件在 iPhone 上看不到

**解决方案**：
1. 等待 iCloud 同步完成（可能需要几分钟）
2. 在 iPhone 的"文件"App 中检查 iCloud Drive
3. 确保 Obsidian 有访问 iCloud Drive 的权限

### 问题：权限错误

**解决方案**：
```bash
chmod +x sync_to_icloud.sh
```

## 自动同步（可选）

如果希望定期自动同步，可以使用 `cron` 或 `launchd`：

### 使用 cron（每小时同步一次）

```bash
crontab -e
```

添加以下行：
```
0 * * * * /Users/longjunwang/dev/GitHub/crp/sync_to_icloud.sh >> /tmp/icloud_sync.log 2>&1
```

### 使用 launchd（macOS 推荐）

创建 `~/Library/LaunchAgents/com.shaderlib.sync.plist`：

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.shaderlib.sync</string>
    <key>ProgramArguments</key>
    <array>
        <string>/Users/longjunwang/dev/GitHub/crp/sync_to_icloud.sh</string>
    </array>
    <key>StartInterval</key>
    <integer>3600</integer>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>
```

然后加载：
```bash
launchctl load ~/Library/LaunchAgents/com.shaderlib.sync.plist
```

## 相关文件

- 脚本位置：`/Users/longjunwang/dev/GitHub/crp/sync_to_icloud.sh`
- 文档位置：`/Users/longjunwang/dev/GitHub/crp/docs/ShaderLibrary/`

