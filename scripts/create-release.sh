#!/bin/bash

# 快速创建release的脚本

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Aictionary Release Creator${NC}"
echo ""

# 检查是否在git仓库中
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo -e "${RED}❌ 错误：当前目录不是git仓库${NC}"
    exit 1
fi

# 检查是否有未提交的更改
if ! git diff-index --quiet HEAD --; then
    echo -e "${YELLOW}⚠️  警告：有未提交的更改${NC}"
    echo "请先提交所有更改后再创建release"
    git status --short
    exit 1
fi

# 获取当前分支
CURRENT_BRANCH=$(git branch --show-current)
if [ "$CURRENT_BRANCH" != "main" ] && [ "$CURRENT_BRANCH" != "master" ]; then
    echo -e "${YELLOW}⚠️  警告：当前不在main/master分支 (当前: $CURRENT_BRANCH)${NC}"
    read -p "是否继续？(y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# 获取最新标签
LATEST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")
echo -e "${BLUE}📍 最新标签: $LATEST_TAG${NC}"

# 提示输入新版本
echo ""
echo "请输入新版本号 (格式: v1.0.0):"
read -p "版本号: " NEW_VERSION

# 验证版本号格式
if [[ ! $NEW_VERSION =~ ^v[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9]+)?$ ]]; then
    echo -e "${RED}❌ 错误：版本号格式不正确${NC}"
    echo "正确格式: v1.0.0 或 v1.0.0-beta"
    exit 1
fi

# 检查标签是否已存在
if git rev-parse "$NEW_VERSION" >/dev/null 2>&1; then
    echo -e "${RED}❌ 错误：标签 $NEW_VERSION 已存在${NC}"
    exit 1
fi

# 显示将要创建的release信息
echo ""
echo -e "${GREEN}📋 Release信息:${NC}"
echo "  版本: $NEW_VERSION"
echo "  分支: $CURRENT_BRANCH"
echo "  提交: $(git rev-parse --short HEAD)"
echo ""

# 确认创建
read -p "确认创建release？(y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "已取消"
    exit 0
fi

# 创建标签
echo -e "${BLUE}🏷️  创建标签 $NEW_VERSION...${NC}"
git tag -a "$NEW_VERSION" -m "Release $NEW_VERSION"

# 推送标签
echo -e "${BLUE}📤 推送标签到远程仓库...${NC}"
git push origin "$NEW_VERSION"

echo ""
echo -e "${GREEN}✅ Release创建成功！${NC}"
echo ""
echo -e "${BLUE}📝 接下来的步骤:${NC}"
echo "1. 查看GitHub Actions页面确认构建状态"
echo "2. 构建完成后，在GitHub Releases页面编辑发布说明"
echo "3. 可以使用 .github/RELEASE_TEMPLATE.md 作为模板"
echo ""
echo -e "${BLUE}🔗 相关链接:${NC}"
echo "  GitHub Actions: https://github.com/$(git config --get remote.origin.url | sed 's/.*github.com[:/]\([^.]*\).*/\1/')/actions"
echo "  Releases: https://github.com/$(git config --get remote.origin.url | sed 's/.*github.com[:/]\([^.]*\).*/\1/')/releases"
echo ""
echo -e "${YELLOW}⏳ 构建大约需要10-15分钟完成${NC}"