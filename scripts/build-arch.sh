#!/bin/bash
set -e

echo "Building Arch Linux package..."

# Generate PKGBUILD
./scripts/generate-pkgbuild.sh

# Create a simplified PKGBUILD for local build
cat > PKGBUILD << 'EOF'
# Maintainer: Aictionary Team
pkgname=aictionary
pkgver=1.0.0
pkgrel=1
pkgdesc="快速且异常好用的词典 App"
arch=('x86_64' 'aarch64')
url="https://github.com/username/Aictionary"
license=('MIT')
depends=()
provides=('aictionary')

package() {
    # Create directories
    install -dm755 "${pkgdir}/usr/bin"
    install -dm755 "${pkgdir}/usr/share/applications"
    install -dm755 "${pkgdir}/usr/share/pixmaps"
    
    # Install binary based on architecture
    if [ "$CARCH" = "x86_64" ]; then
        if [ -f "${srcdir}/../artifacts/linux-amd64/Aictionary" ]; then
            install -Dm755 "${srcdir}/../artifacts/linux-amd64/Aictionary" "${pkgdir}/usr/bin/aictionary"
        fi
    elif [ "$CARCH" = "aarch64" ]; then
        if [ -f "${srcdir}/../artifacts/linux-arm64/Aictionary" ]; then
            install -Dm755 "${srcdir}/../artifacts/linux-arm64/Aictionary" "${pkgdir}/usr/bin/aictionary"
        fi
    fi
    
    # Install desktop file
    cat > "${pkgdir}/usr/share/applications/aictionary.desktop" << 'DESKTOP'
[Desktop Entry]
Name=Aictionary
Comment=快速且异常好用的词典 App
Exec=/usr/bin/aictionary
Icon=aictionary
Terminal=false
Type=Application
Categories=Office;Dictionary;
DESKTOP
    
    # Install icon if available
    if [ -f "${srcdir}/../Aictionary/Assets/AppIcon.png" ]; then
        install -Dm644 "${srcdir}/../Aictionary/Assets/AppIcon.png" "${pkgdir}/usr/share/pixmaps/aictionary.png"
    fi
}
EOF

# Build package
makepkg -f --noconfirm

echo "Arch Linux package built successfully!"
ls -la *.pkg.tar.zst