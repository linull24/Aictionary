#!/bin/bash

# 验证GitHub Actions工作流配置的脚本

set -e

echo "🔍 验证 GitHub Actions 工作流配置"

WORKFLOW_FILE=".github/workflows/build.yml"

if [ ! -f "$WORKFLOW_FILE" ]; then
    echo "❌ 工作流文件不存在: $WORKFLOW_FILE"
    exit 1
fi

echo "✅ 工作流文件存在"

# 检查工作流语法
echo "📋 检查工作流语法..."

# 检查是否包含release job
if grep -q "release:" "$WORKFLOW_FILE"; then
    echo "✅ 包含 release job"
else
    echo "❌ 缺少 release job"
    exit 1
fi

# 检查是否有tag触发器
if grep -q "tags:" "$WORKFLOW_FILE"; then
    echo "✅ 包含 tag 触发器"
else
    echo "❌ 缺少 tag 触发器"
    exit 1
fi

# 检查是否有必要的权限
if grep -q "contents: write" "$WORKFLOW_FILE"; then
    echo "✅ 包含必要的权限设置"
else
    echo "❌ 缺少必要的权限设置"
    exit 1
fi

# 检查是否有创建release的步骤
if grep -q "create-release" "$WORKFLOW_FILE"; then
    echo "✅ 包含创建 release 的步骤"
else
    echo "❌ 缺少创建 release 的步骤"
    exit 1
fi

# 检查是否有上传资产的步骤
if grep -q "upload-release-asset" "$WORKFLOW_FILE"; then
    echo "✅ 包含上传资产的步骤"
else
    echo "❌ 缺少上传资产的步骤"
    exit 1
fi

echo ""
echo "🎉 工作流配置验证完成！"
echo ""
echo "📝 要测试release功能，请执行以下步骤："
echo "1. 确保代码已提交到main分支"
echo "2. 创建并推送标签："
echo "   git tag v1.0.0"
echo "   git push origin v1.0.0"
echo "3. 查看GitHub Actions页面确认工作流运行"
echo "4. 检查Releases页面确认release创建成功"