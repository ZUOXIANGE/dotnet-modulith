using System.Text.RegularExpressions;

var argsList = Args.ToList();
if (argsList.Count == 0)
{
    Console.WriteLine("ERROR: No commit message file path provided.");
    return 1;
}

var commitMsgFile = argsList[0];
if (!File.Exists(commitMsgFile))
{
    Console.WriteLine($"ERROR: Commit message file not found: {commitMsgFile}");
    return 1;
}

var commitMsg = File.ReadAllText(commitMsgFile).Trim();

if (string.IsNullOrWhiteSpace(commitMsg))
{
    Console.WriteLine("ERROR: Commit message is empty.");
    return 1;
}

var pattern = @"^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\(.+\))?: .{1,100}";

if (!Regex.IsMatch(commitMsg, pattern))
{
    Console.WriteLine("ERROR: Invalid commit message format.");
    Console.WriteLine();
    Console.WriteLine("Expected format: <type>(<scope>): <subject>");
    Console.WriteLine();
    Console.WriteLine("Types: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  feat(orders): add order cancellation endpoint");
    Console.WriteLine("  fix(inventory): correct stock reservation logic");
    Console.WriteLine("  chore: update dependencies");
    return 1;
}

Console.WriteLine("Commit message format validated.");
return 0;
