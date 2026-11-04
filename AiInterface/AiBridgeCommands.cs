// runtime-bridge 适配：把 AI 命令行暴露成 ai.* 命令，供外部经 TCP 调用（命令在主线程同步执行）
using UnityRuntimeBridge;

[UnityEngine.Scripting.Preserve]
public static class AiBridgeCommands
{
    private static AiCommandLine CommandLine => Tool.ClientLogicManager != null ? Tool.ClientLogicManager.Ai : null;

    [RuntimeCommand("ai.exec", "执行一条 AI 命令行命令；参数 line")]
    public static object Exec(RuntimeBridgeContext ctx, RuntimeBridgeArgs args)
    {
        var ai = CommandLine;
        if (ai == null) return "错误：AI 命令行未就绪（当前进程没有客户端场景）";

        string line = args.GetString("line", "");
        if (line.Trim().Length == 0) return "错误：缺少参数 line";

        // 桥一次调用一次响应，阻塞语义只能由 ai.wait 轮询实现
        if (line.TrimStart().StartsWith("等待中断")) return "错误：「等待中断」请改用 ai.wait";

        string reply = null;
        ai.Submit(line, text => reply = text);
        return reply ?? "错误：命令没有立即返回";
    }

    [RuntimeCommand("ai.interrupts", "取走中断原因列表，没有则返回空字符串")]
    public static object Interrupts(RuntimeBridgeContext ctx, RuntimeBridgeArgs args)
    {
        var ai = CommandLine;
        if (ai == null) return "错误：AI 命令行未就绪（当前进程没有客户端场景）";
        return ai.Interrupts.TryDrain(out string text) ? text : "";
    }
}
