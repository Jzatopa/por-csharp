using System.Text.RegularExpressions;
namespace Por.UpstreamTools;
public sealed record ScenarioStep(int Line,string Scope,string Command,string Arguments);
public static class Scenario
{
 static readonly HashSet<string> Commands=new(){"boot","press","wait","checkpoint","saves","ignore","mask","screens","put","strip"};
 public static List<ScenarioStep> Parse(string text)
 {
  var steps=new List<ScenarioStep>();int line=0;
  foreach(string raw in text.Split('\n')){
   line++;string input=raw.Trim();if(input.Length==0||input.StartsWith('#'))continue;
   // The original format's comments start with #, including after a step.
   int comment=input.IndexOf('#');if(comment>=0)input=input[..comment].TrimEnd();
   string scope="both";
   foreach(string prefix in new[]{"golden:","candidate:"})if(input.StartsWith(prefix)){scope=prefix[..^1];input=input[prefix.Length..].TrimStart();break;}
   int space=input.IndexOf(' ');string command=space<0?input:input[..space],args=space<0?"":input[(space+1)..].Trim();
   if(!Commands.Contains(command))throw new InvalidDataException($"Unknown scenario command at line {line}: {command}");
   if(command!="boot"&&args.Length==0)throw new InvalidDataException($"Missing {command} arguments at line {line}");
   if(command=="screens"&&args!="off"&&args!="on")throw new InvalidDataException("screens expects on/off");
   if(command=="wait"){
    string pattern=Regex.Replace(args,@"\s+\d+$","");_ = new Regex(pattern);
   }
   if(command=="ignore"||command=="strip")_ = new Regex(args);
   steps.Add(new(line,scope,command,args));
  }
  if(steps.Count==0||steps[0].Command!="boot")throw new InvalidDataException("Scenario must begin with boot");
  return steps;
 }
}
