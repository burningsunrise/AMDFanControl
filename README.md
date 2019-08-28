# AMDGPU Fancontrol - .Net Core

This is a .Net core application that takes automatic control of your GPU fans for AMDGPU drivers on *Nix machines. This project is completely automated and needs no input from the user other than building and creating the appropriate services on your own machine. The goal of this project is to extend the life of your AMD fans and to optimize at what temperature they spin up and down, and how often they read and write to the disk. This fancontroller uses Async file access to try and read and write to files efficiently. I have not found any fan controllers that work with AMDGPU drivers and are automatic for *Nix machines so I decided to make one that is robust enough for my needs. By reducing writes to disk we are utilizing a lot less CPU not to mention if you run a machine off a USB this helps the longevity of the USB, and by checking the folders on startup rather than every time we run the loop we are taking a lot less time to find the correct folders.

## Usage

To use, simply copy the git and build for your desired system, **MUST RUN AS SUDO**. You can change any variables you wish the most promenant ones would probably be PrecentageRange and WithinPercentage range, but nothing is necessary to get this to run. After you have built, you can either run manually on your machine, or create a systemd service file like below:

```text
Description=Automatic Fancontrol

[Service]
Type=simple
PIDFile=/run/fancontroller.pid
ExecStart=/opt/dotnet/dotnet /home/user/fancontrol/Fancontrol.dll
User=root
Group=root
WorkingDirectory=/home/user/fancontrol/
Restart=always
RestartSec=10
SyslogIdentifier=fancontrol

[Install]
WantedBy=multi-user.target
```

If there are any questions, bugs, or feature requests please open an issue on gitlab! :dog:
