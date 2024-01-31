<div align="center" style="border: 1px solid black; padding: 10px;">

[English](./README.md) / 简体中文

</div>

## 描述
这是一个简易的服务器管理程序，它的功能包括：

- 定时备份（默认每10分钟）
- 当主机可用内存不足时自动重启
- 将备份、重启和内存状态事件推送到Kook频道
- 支持简单命令
- 如果你有任何实用功能的想法，请通过`Issues`让我知道。owo

该项目是在Linux上开发和确保其运行的，**主要面向Linux系统**。我也开发了Windows操作逻辑，但我**无法保证**它能在Windows服务器上正常运行。

## 使用方法

### 从二进制发行版安装
1. 将可执行文件和`config.xml`解压到您喜欢的位置。
2. 在`config.xml`中配置必要的设置。
3. 运行可执行文件。我建议您使用像`tmux`或`screen`这样的终端多路复用器，在一个专用会话中运行它。

### 从源代码安装
⚠️ **构建/发布此项目需要`git`和`dotnet-sdk-6.0`；如果您的系统上没有这些需求工具，您需要先安装它们。**
1. 克隆此项目到本地
```shell
mkdir -p ~/Downloads/PalWorldServerMgmt; cd $_ # 项目将被克隆到这个位置
git clone https://github.com/Illusion2-1/PalWorldServerMgmt .
```

2. 发布
   *如果你不想包含运行时，请将--self-contained标志设置为false*
   *这是发布Linux可执行文件的示例。要发布Windows可执行文件，请将RID `linux-x64`更改为`win-x64`*
```shell
dotnet publish -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true
```
3. 如果构建成功，您可以从MSBuild输出中找到发布位置。通常它会在`./bin/Release/net6.0/linux-x64/publish/`内。
   请注意，配置文件`src/config.xml`需要与可执行文件在同一目录下。您需要将其移动到可执行文件所在的同一目录。
4. 在`config.xml`中配置必要的设置。
5. 运行可执行文件。我建议您使用像`tmux`或`screen`这样的终端多路复用器，在一个专用会话中运行它。

## 配置
您需要确保您的配置完全正确，以使守护程序正常运行。
请按照下面的说明配置您的守护程序
```xaml
<!--这是Linux平台的配置。在Windows上表示路径的方式可能有所不同，请在复制粘贴时注意-->
<Config>
    <Kook>
    <!--KookEnable: 将此设置为true将启用将事件推送到kook频道的功能。
    如果您不需要此功能，请将其留空或设置为false
    -->
        <KookEnable></KookEnable>
    <!--BaseUrl: 如果您不使用自定义端点或反向代理，请将其留空-->
        <BaseUrl></BaseUrl>
    <!--RequestPath: 如果您不使用自定义端点或反向代理，请将其留空-->
        <RequestPath></RequestPath>
    <!--UseSSL: 如果您不使用自定义端点或反向代理，请将其留空-->
        <UseSSL></UseSSL>
    <!--Authorization: 从https://developer.kookapp.cn/app/index获得的令牌。不要忘了添加令牌类型bot。
        <Authorization>Bot 你的机器人的TOKEN</Authorization>
    <!--如果你不知道你在做什么，就将其留为1-->
        <PostType>1</PostType>
    <!--您的频道id。可以通过进入您的频道后，Url链接最后一段的ID获得-->
        <TargetID></TargetID>
    <!--如果您想在每条消息中添加额外的后缀，机会来了xd-->
        <CustomContent></CustomContent>
    </Kook>
    <PalWorld>
    <!--不要将路径留空，并确保您有权访问这些目录-->
    <!--您的游戏保存路径。我建议使用Saved-->
        <SrcFolder>/home/illusion/PalWorld/Pal/Saved</SrcFolder>
    <!--备份存档应该放在哪里-->
        <TgtFolder>/home/illusion/PalWorld/Backup</TgtFolder>
    <!--运行steamcmd的可执行文件或.sh脚本-->
        <SteamCmdExecPath>home/illusion/steamcmd/steamcmd.sh</SteamCmdExecPath>
    <!--运行PalWorld服务器的可执行文件或.sh脚本-->
        <PalWorldExecPath>/home/illusion/PalWorld/PalServer.sh</PalWorldExecPath>
    <!--从0.0到1.0。可以为空，为空时，默认为0.9-->
        <MemThreshold></MemThreshold>
    <!--从1到65535。可以为空，为空时，默认为8211-->
        <CustomPort></CustomPort>
    <!--我们是否应该在每次运行守护程序时检查服务器更新？-->
        <DoUpdate>true</DoUpdate>
    </PalWorld>
</Config>
```

## 命令
我们有一些内置命令供您使用：

- `exit` - 立即退出PalWorld服务器和守护程序。
- `backup` - 立即创建备份。
- `restart` - 立即重启PalWorld服务器。
- `stats`- 显示当前内存使用情况。
- `echo [message]` - 向Kook频道发送自定义消息（仅当`KookEnable`设置为`true`时有效）。
