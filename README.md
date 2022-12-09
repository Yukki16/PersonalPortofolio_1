# PersonalPortofolio_1
First term, year 2, personal portofolio Unity project, Terrain generation with perlin noise and Unity Networking.

The networking is at the begining and can only be used locally. No interface added to it yet so to access the project do the following steps:
1. Make the build
2. Save the build somewhere like on Desktop, for the example I use it as the build is on desktop
3. Open a Command Prompt (Windows key + R and type "cmd")
4. Write/copy-paste the next command into the command prompt, it will open both a server and a client
Desktop\name of the folder you put the build in\PersonalPortofolio1.exe -mlapi server & Desktop\name of the folder you put the build in\PersonalPortofolio1.exe -mlapi client

You can connect as many clients as you want after openning a server, but camera moves to the newest client connected and all the players move with ur input.

As following terms I am looking to add an user-friendly interface to be able to open the client/server easier and to connect the project to Unity Relay so I can test
the project online and not locally.
