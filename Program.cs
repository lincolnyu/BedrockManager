void Run()
{
}

void ShowHelp()
{
}

if (args.Length > 0)
{
    switch (args[0])
    {
        case "run":
            Run();
            break;
        case "help":
            ShowHelp();
            break;
    }
}
else
{
    ShowHelp();
}
