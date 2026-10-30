# 获取脚本自身的目录，并切换到该目录下的 'fantnel' 子目录
cd "$(dirname "$0")/fantnel";

# 使用 dotnet 命令运行当前目录下的 'Fantnel.dll'
dotnet Fantnel.dll;