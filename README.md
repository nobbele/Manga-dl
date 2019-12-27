# Manga-dl

## Table of Contents  
[About](#about)  
[Building](#building)  
[Usage](#usage)  

## <a name="about">About</a>
This manga downloader currently only supports mangadex but there are plans for more servers and even light novels and anime.
There's currently only a CLI but a GUI is planned.
## <a name="building">Building</a>
Dependencies: .Net Core SDK and Runtime

## Cloning from remote

    git clone https://github.com/nobbele/Manga-dl.git
    cd manga-dl

## Building CLI

    dotnet build Console/manga-dl.Console.csproj

Binaries located in 

Console/bin/[Debug|Release]/netcoreapp[Version]/manga-dl.Console.dll

For example, a release build on .net core 2.2 would be located in 

Console/bin/Release/netcoreapp2.2/manga-dl.Console.dll

## GUI

TBD

## <a name="usage">Usage</a>
[] = required

() = optional unless there's another optional after it, then it's required

    dotnet [path] manga [mangadex-id as number] (concurrent api access as bool)

The manga will be downloaded in current directory so it's advised to move the binaries to another location and cd there

For example

    dotnet manga-dl.Console.dll manga 23439

PDFs will be placed in Mangas/[Manga name]/[volume]-[chapter].pdf

PNGs will be placed in Mangas/[Manga name]/[volume]-[chapter]/page[page number].png