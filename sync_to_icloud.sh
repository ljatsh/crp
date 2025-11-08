#!/bin/bash

# Unity Shader Library 文档同步到 iCloud Drive 脚本
# 使用方法: ./sync_to_icloud.sh

set -e  # 遇到错误立即退出

# 颜色定义
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# 配置路径
SOURCE_DIR="$HOME/dev/GitHub/crp/docs/ShaderLibrary"
ICLOUD_BASE="$HOME/Library/Mobile Documents/com~apple~CloudDocs"
TARGET_DIR="$ICLOUD_BASE/Obsidian/ShaderLibrary"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Unity Shader Library 同步脚本${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# 检查源目录是否存在
if [ ! -d "$SOURCE_DIR" ]; then
    echo -e "${RED}错误: 源目录不存在${NC}"
    echo "  路径: $SOURCE_DIR"
    exit 1
fi

echo -e "${GREEN}✓${NC} 源目录检查通过: $SOURCE_DIR"
echo ""

# 检查 iCloud Drive 是否存在
if [ ! -d "$ICLOUD_BASE" ]; then
    echo -e "${RED}错误: iCloud Drive 未找到${NC}"
    echo "  路径: $ICLOUD_BASE"
    echo "  请确保已启用 iCloud Drive"
    exit 1
fi

echo -e "${GREEN}✓${NC} iCloud Drive 检查通过"
echo ""

# 创建目标目录（如果不存在）
mkdir -p "$TARGET_DIR"
echo -e "${GREEN}✓${NC} 目标目录已准备: $TARGET_DIR"
echo ""

# 统计文件数量
SOURCE_COUNT=$(find "$SOURCE_DIR" -name "*.md" -type f | wc -l | tr -d ' ')
echo "源文件数量: $SOURCE_COUNT 个 Markdown 文件"
echo ""

# 使用 rsync 同步文件（保留时间戳和权限）
echo -e "${YELLOW}开始同步文件...${NC}"
rsync -av --delete \
    --exclude=".DS_Store" \
    --exclude="*.swp" \
    --exclude="*.tmp" \
    "$SOURCE_DIR/" "$TARGET_DIR/"

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}同步完成！${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "源目录: $SOURCE_DIR"
echo "目标目录: $TARGET_DIR"
echo ""

# 验证同步结果
TARGET_COUNT=$(find "$TARGET_DIR" -name "*.md" -type f | wc -l | tr -d ' ')
echo "已同步文件数量: $TARGET_COUNT 个 Markdown 文件"

if [ "$SOURCE_COUNT" -eq "$TARGET_COUNT" ]; then
    echo -e "${GREEN}✓${NC} 文件数量匹配"
else
    echo -e "${YELLOW}⚠${NC} 文件数量不匹配（源: $SOURCE_COUNT, 目标: $TARGET_COUNT）"
fi

echo ""
echo "下一步操作："
echo "1. 在 iPhone 上打开 Obsidian"
echo "2. 设置 → 文件与链接 → 打开其他库"
echo "3. 选择 iCloud Drive → Obsidian → ShaderLibrary"
echo ""
echo "提示：文件会自动通过 iCloud 同步到 iPhone"
