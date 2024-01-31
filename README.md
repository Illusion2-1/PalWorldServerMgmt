<div align="center" style="border: 1px solid black; padding: 10px;">

English / [简体中文](./README_CN.md)

</div>

## Description
This is a simple server manager program that features

- scheduled backup (10 min by default)
- auto restart when the available memory of the host is insufficient
- pushing backup, restart and memory status events to Kook channel
- Simple commands support
- If your have any idea adding practical features, plz let me know via the `Issues`. owo

The project was developed, assured on Linux and **targeted Linux**. I conveniently developed the Windows operating logic, but I **cannot guarantee** that it 
will 
run properly on a Windows server.
## Usage

### From binary release
1. Extract executable file and `config.xml` both to your favorite place.
2. Configure necessary settings in `config.xml`.
3. Run the executable. I would recommend you to run it inside a dedicated session using terminal multiplexer like `tmux` or `screen`.

### From source code
⚠️ **`git` and `dotnet-sdk-6.0` is required to build/publish this project; if you haven't had requirements on your system, you will need to install them 
first.**
1. Clone this project to local
```shell
mkdir -p ~/Downloads/PalWorldServerMgmt; cd $_ # project will be cloned to this place
git clone https://github.com/Illusion2-1/PalWorldServerMgmt .
```

2. Publish  
*flag --self-contained to false if you don't want to contain runtime in your release*
*this is a example for publishing Linux executable. To publish a Windows executable, change the RID `linux-x64` to `win-x64`*
```shell
dotnet publish -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true
```
3. You will be able to find where the publish locates from MSBuild output if it is built successfully. Normally it will be inside`./bin/Release/net6.
   0/linux-x64/publish/`.   
Note that the configuration file `src/config.xml` needs to be in the same directory as the executable file. You need to move it to the same directory as the executable file.
4. Configure necessary settings in `config.xml`.
5. Run the executable. I would recommend you to run it inside a dedicated session using terminal multiplexer like `tmux` or `screen`.

## Configurations
You will need to make sure you have absolutely correct configuration to make the daemon run properly.  
Please follow the explanation below to configure your daemon
```xaml
<!--This is a configuration of Linux platform. The way to illustrate path may vary on Windows, please be aware of it when copy and paste-->
<Config>
    <Kook>
    <!--KookEnable: making this true will enable the function pushing events to kook channel.
    If you do not need this feature, leave it NULL of false
    -->
        <KookEnable></KookEnable>
    <!--BaseUrl: Leave it NULL if you're not using a custom endpoint or reverse proxy-->
        <BaseUrl></BaseUrl>
    <!--RequestPath: Leave it NULL if you're not using a custom endpoint or reverse proxy-->
        <RequestPath></RequestPath>
    <!--UseSSL: Leave it NULL if you're not using a custom endpoint or reverse proxy-->
        <UseSSL></UseSSL>
    <!--Authorization: Token you get from https://developer.kookapp.cn/app/index. Don't forget to add token type bot.
        <Authorization>Bot TOKEN_OF_YOUR_BOT</Authorization>
    <!--Leave it 1 if you have no idea what you are doing-->
        <PostType>1</PostType>
    <!--Your channel id. It can be obtained by the ID of the last segment of the Url link after entering the your channel-->
        <TargetID></TargetID>
    <!--If you want to add addition suffix to every message, here's your chance xd-->
        <CustomContent></CustomContent>
    </Kook>
    <PalWorld>
    <!--Do not leave paths NULL and make sure you have proper permission to these directories-->
    <!--Path to your game saves. I would recommend Saved-->
        <SrcFolder>/home/illusion/PalWorld/Pal/Saved</SrcFolder>
    <!--Path to what place should backup archives be-->
        <TgtFolder>/home/illusion/PalWorld/Backup</TgtFolder>
    <!--Executable or .sh script that runs steamcmd-->
        <SteamCmdExecPath>home/illusion/steamcmd/steamcmd.sh</SteamCmdExecPath>
    <!--Executable or .sh script that runs PalWorld server-->
        <PalWorldExecPath>/home/illusion/PalWorld/PalServer.sh</PalWorldExecPath>
    <!--from 0.0 to 1.0. It can be null, when so, default to 0.9-->
        <MemThreshold></MemThreshold>
    <!--from 1 to 65535. It can be null, when so, default to 8211-->
        <CustomPort></CustomPort>
    <!--Should we check for server update every time run daemon?-->
        <DoUpdate>true</DoUpdate>
    </PalWorld>
</Config>
```

## Commands
We have some built-in commands that you can use:

- `exit` - Exit PalWorld server and daemon immediately.
- `backup` - Create a backup immediately.
- `restart` - Restart PalWorld server immediately.
- `stats`- Display current memory usage
- `echo [message]` - Send a custom message to Kook channel (Only works when `KookEnable` is set to `true`)