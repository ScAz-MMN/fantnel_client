Ubuntu美化与基础开发环境搭建指南

这份文档将带你完成Ubuntu系统的桌面美化，以及一套完整的基础开发环境配置，所有命令均经过实测验证，可直接复制执行。

一、系统基础优化
更换国内软件源‌
将默认软件源替换为清华镜像站，大幅提升下载速度：
bash
sudo sed -i 's@//.*archive.ubuntu.com@//mirrors.tuna.tsinghua.edu.cn@g' /etc/apt/sources.list.d/ubuntu.sources
sudo apt update && sudo apt upgrade -y

安装基础系统工具‌
安装日常开发必备的系统工具集：
bash
sudo apt install -y curl wget git vim htop neofetch build-essential

启用SSH远程连接‌
开启SSH服务，实现跨设备远程管理：
bash
sudo apt install -y openssh-server
sudo systemctl enable ssh --now

二、系统美化配置
安装GNOME扩展管理器‌
先安装必要的依赖和扩展管理工具：
bash
sudo apt install -y gnome-tweaks chrome-gnome-shell

终端美化：Zsh + Oh My Zsh‌
替换默认终端为功能更强大的Zsh：
bash
sudo apt install -y zsh
sh -c "$(curl -fsSL https://raw.githubusercontent.com/ohmyzsh/ohmyzsh/master/tools/install.sh)"

安装终端增强插件‌
添加自动补全和语法高亮功能：
bash
git clone https://github.com/zsh-users/zsh-autosuggestions ${ZSH_CUSTOM:-~/.oh-my-zsh/custom}/plugins/zsh-autosuggestions
git clone https://github.com/zsh-users/zsh-syntax-highlighting ${ZSH_CUSTOM:-~/.oh-my-zsh/custom}/plugins/zsh-syntax-highlighting


编辑~/.zshrc文件，在plugins=()中添加插件：

bash
plugins=(git zsh-autosuggestions zsh-syntax-highlighting)

桌面视觉美化‌
打开GNOME Tweaks工具，可自定义主题、图标、光标样式，推荐从GNOME Extensions网站安装Dash to Dock、Blur My Shell等扩展，实现类MacOS的桌面效果。
三、基础开发环境搭建
安装VS Code编辑器‌
添加微软官方软件源并安装代码编辑器：
bash
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > packages.microsoft.gpg
sudo install -D -o root -g root -m 644 packages.microsoft.gpg /etc/apt/keyrings/packages.microsoft.gpg
sudo sh -c 'echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/packages.microsoft.gpg] https://packages.microsoft.com/repos/code stable main" > /etc/apt/sources.list.d/vscode.list'
sudo apt update && sudo apt install -y code

安装Docker容器环境‌
一键安装Docker并配置免sudo使用：
bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER

Python开发环境配置‌
安装Python包管理工具，推荐使用uv提升依赖安装速度：
bash
sudo apt install -y python3-pip python3-venv
pip install uv

NVIDIA显卡驱动配置（可选）‌
如有NVIDIA显卡，安装官方闭源驱动以支持GPU开发：
bash
sudo apt install -y nvidia-driver-550
sudo reboot


重启后执行nvidia-smi验证驱动安装成功。

四、中文环境适配
安装Fcitx5中文输入法‌
解决Linux下中文输入痛点：
bash
sudo apt install -y fcitx5 fcitx5-chinese-addons fcitx5-frontend-gtk3


在系统设置的「Region & Language」中添加Fcitx5作为输入框架，重启后即可正常使用中文输入。

完成以上步骤后，你将得到一个界面美观、开发工具齐全的Ubuntu工作站，完全满足日常开发和学习需求。