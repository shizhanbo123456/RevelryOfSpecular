-- 命令行拓展：ai.register_command(名字, 处理函数)，命令行统一用「执行lua拓展内容 名字 参数」调用
-- 处理函数收一个字符串参数（空格拼接的参数串），返回字符串作为命令结果
ai.register_command("ai_hello", function(args)
    return "hello: " .. tostring(args) .. " | 相位=" .. ai.phase()
end)
