#!/bin/bash
set -e

VERSION=$(grep -o '<Version>[^<]*' Aictionary/Aictionary.csproj | sed 's/<Version>//')
if [ -z "$VERSION" ]; then
    VERSION="1.0.0"
fi

build_deb() {
    local arch=$1
    local artifact_dir=$2
    local deb_arch=$3
    
    echo "Building DEB package for $arch..."
    
    # Create package structure
    local pkg_dir="aictionary-${VERSION}-${arch}"
    mkdir -p "$pkg_dir/DEBIAN"
    mkdir -p "$pkg_dir/usr/bin"
    mkdir -p "$pkg_dir/usr/share/applications"
    mkdir -p "$pkg_dir/usr/share/pixmaps"
    
    # Copy binary
    if [ -f "artifacts/$artifact_dir/Aictionary" ]; then
        cp "artifacts/$artifact_dir/Aictionary" "$pkg_dir/usr/bin/"
        chmod +x "$pkg_dir/usr/bin/Aictionary"
    else
        echo "Warning: Binary not found in artifacts/$artifact_dir/"
        return 1
    fi
    
    # Create control file
    cat > "$pkg_dir/DEBIAN/control" << EOF
Package: aictionary
Version: $VERSION
Section: utils
Priority: optional
Architecture: $deb_arch
Maintainer: Aictionary Team
Description: 快速且异常好用的词典 App
 基于 Avalonia 框架的跨平台词典应用程序，支持本地词库和大语言模型驱动的词义生成。
EOF
    
    # Create desktop file
    cat > "$pkg_dir/usr/share/applications/aictionary.desktop" << EOF
[Desktop Entry]
Name=Aictionary
Comment=快速且异常好用的词典 App
Exec=/usr/bin/Aictionary
Icon=aictionary
Terminal=false
Type=Application
Categories=Office;Dictionary;
EOF
    
    # Copy icon if exists
    if [ -f "Aictionary/Assets/AppIcon.png" ]; then
        cp "Aictionary/Assets/AppIcon.png" "$pkg_dir/usr/share/pixmaps/aictionary.png"
    fi
    
    # Build package
    fakeroot dpkg-deb --build "$pkg_dir"
    mv "${pkg_dir}.deb" "aictionary_${VERSION}_${deb_arch}.deb"
    rm -rf "$pkg_dir"
    
    echo "Created: aictionary_${VERSION}_${deb_arch}.deb"
}

# Build for both architectures
build_deb "amd64" "linux-amd64" "amd64"
build_deb "arm64" "linux-arm64" "arm64"

echo "DEB packages built successfully!"