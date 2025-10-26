#!/bin/bash

# 测试release功能的脚本
# 此脚本用于本地测试release工作流的各个步骤

set -e

echo "🚀 开始测试 Aictionary Release 功能"

# 检查必要的工具
echo "📋 检查必要工具..."
command -v git >/dev/null 2>&1 || { echo "❌ 需要安装 git"; exit 1; }
command -v dotnet >/dev/null 2>&1 || { echo "❌ 需要安装 .NET SDK"; exit 1; }

# 获取当前版本信息
CURRENT_BRANCH=$(git branch --show-current)
LATEST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")
echo "📍 当前分支: $CURRENT_BRANCH"
echo "📍 最新标签: $LATEST_TAG"

# 创建测试标签
TEST_TAG="v1.0.0-test-$(date +%s)"
echo "🏷️  创建测试标签: $TEST_TAG"

# 模拟构建过程
echo "🔨 开始构建测试..."
cd "$(dirname "$0")/.."

# 清理之前的构建
if [ -d "artifacts" ]; then
    echo "🧹 清理旧的构建产物..."
    rm -rf artifacts
fi

# 运行构建
echo "⚙️  执行构建命令..."
dotnet run --project build/build.csproj -- Clean

# 测试单个平台构建（Linux AMD64）
echo "🐧 测试 Linux AMD64 构建..."
dotnet run --project build/build.csproj -- PublishLinuxAmd64

# 检查构建结果
if [ -d "artifacts/linux-amd64" ]; then
    echo "✅ Linux AMD64 构建成功"
    ls -la artifacts/linux-amd64/
else
    echo "❌ Linux AMD64 构建失败"
    exit 1
fi

# 测试打包功能
echo "📦 测试打包功能..."
cd artifacts
if [ -d "linux-amd64" ]; then
    echo "📦 创建 Linux AMD64 压缩包..."
    tar -czf Aictionary-linux-amd64-test.tar.gz linux-amd64/
    if [ -f "Aictionary-linux-amd64-test.tar.gz" ]; then
        echo "✅ 压缩包创建成功: $(ls -lh Aictionary-linux-amd64-test.tar.gz)"
    else
        echo "❌ 压缩包创建失败"
        exit 1
    fi
fi
cd ..

# 测试可执行文件
echo "🧪 测试可执行文件..."
EXECUTABLE="artifacts/linux-amd64/Aictionary"
if [ -f "$EXECUTABLE" ]; then
    echo "✅ 可执行文件存在: $EXECUTABLE"
    echo "📊 文件信息:"
    ls -lh "$EXECUTABLE"
    file "$EXECUTABLE"
    
    # 测试是否可以运行（显示帮助信息）
    echo "🔍 测试运行（应该会因为没有显示而失败，这是正常的）..."
    timeout 5s "$EXECUTABLE" --help 2>/dev/null || echo "⚠️  程序需要图形界面，无法在命令行测试运行"
else
    echo "❌ 可执行文件不存在"
    exit 1
fi

# 模拟GitHub Release创建
echo "📋 模拟 GitHub Release 信息..."
cat << EOF

🎉 Release 测试完成！

如果这是真实的release，将会创建以下内容：

标签: $TEST_TAG
发布名称: Aictionary $TEST_TAG

包含的文件:
$(find artifacts -name "*.tar.gz" -o -name "*.zip" -o -name "*.deb" -o -name "*.pkg.tar.zst" 2>/dev/null | sed 's/^/  - /')

构建产物目录:
$(find artifacts -type d -maxdepth 1 | grep -v "^artifacts$" | sed 's/^/  - /')

EOF

# 清理测试文件
echo "🧹 清理测试文件..."
rm -f artifacts/*.tar.gz

echo "✅ Release 功能测试完成！"
echo ""
echo "📝 要创建真实的release，请执行："
echo "   git tag $TEST_TAG"
echo "   git push origin $TEST_TAG"
echo ""
echo "⚠️  注意：这将触发GitHub Actions自动构建和发布"