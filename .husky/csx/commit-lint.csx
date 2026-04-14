using System.Text.RegularExpressions;

if (Args.Count == 0)
{
    Console.WriteLine("ERROR: No commit message file path provided.");
    Environment.Exit(1);
}

var commitMsgFile = Args[0];
if (!File.Exists(commitMsgFile))
{
    Console.WriteLine($"ERROR: Commit message file not found: {commitMsgFile}");
    Environment.Exit(1);
}

var commitMsg = File.ReadAllText(commitMsgFile).Trim();

if (string.IsNullOrWhiteSpace(commitMsg))
{
    Console.WriteLine("ERROR: Commit message is empty.");
    Environment.Exit(1);
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
    Environment.Exit(1);
}

Console.WriteLine("Commit message format validated.");
