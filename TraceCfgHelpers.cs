using System;
using System.Linq;
using System.Collections.Generic;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Data.UtilityNetwork.Trace;

public static class TraceCfgHelpers
{
    public static NetworkAttribute FindNetworkAttribute(UtilityNetworkDefinition def, params string[] candidateNames)
    {
        if (def == null || candidateNames == null || candidateNames.Length == 0) return null;

        // 归一化：去掉空白与下划线，仅保留字母数字，转小写
        string Norm(string s) => new string((s ?? "")
            .Where(ch => char.IsLetterOrDigit(ch))
            .ToArray())
            .ToLowerInvariant();

        // 候选名集合（已归一化）
        var wanted = candidateNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(Norm)
            .ToHashSet();

        // 常见别名兜底（可按需扩展）
        var alias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // 左边是你传入的名字（归一化前），右边是可能在库里出现的别称
        { "AssetType", "Asset Type" },
        { "Asset Type", "AssetType" },
        { "LifeCycleStatus", "Life Cycle Status" },
        { "Life Cycle Status", "LifeCycleStatus" },
    };
        foreach (var name in candidateNames.ToArray())
        {
            if (alias.TryGetValue(name ?? "", out var alt))
                wanted.Add(Norm(alt));
        }

        var attrs = def.GetNetworkAttributes();
        // 先精确（归一化后）匹配
        var match = attrs.FirstOrDefault(a => wanted.Contains(Norm(a.Name)));
        if (match != null) return match;

        // 再做一次宽松“包含式”匹配（比如你给 LifeCycleStatus，它在库里可能是 LifeCycleStatusCode）
        match = attrs.FirstOrDefault(a =>
        {
            var an = Norm(a.Name);
            return wanted.Any(w => an.Contains(w) || w.Contains(an));
        });

        return match; // 仍找不到就返回 null
    }

    /// <summary>
    /// 从 Traversability.Barriers 表达式树中移除指定 NetworkAttribute 的比较（如 NormalOperatingStatus），
    /// 并对逻辑表达式做结构化简（剔除空节点、单支提升）。
    /// </summary>
    /// <param name="barriers">cfg.Traversability.Barriers</param>
    /// <param name="attributeNames">
    /// 要移除的网络属性名（不区分大小写，自动忽略空格/下划线）。
    /// 例如： "NormalOperatingStatus", "Normal Status", "NORMAL_OPERATING_STATUS"
    /// </param>
    public static Condition RemoveAttrFromBarriers(
        Condition barriers,
        params string[] attributeNames)
    {
        if (barriers == null) return null;

        // 默认目标：NormalOperatingStatus 常见几种命名
        if (attributeNames == null || attributeNames.Length == 0)
            attributeNames = new[] { "NormalOperatingStatus", "Normal Status", "NORMAL_OPERATING_STATUS" };

        // 归一化对比：忽略大小写、空格、下划线
        string Normalize(string s) => (s ?? string.Empty)
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty);

        var nameSet = new HashSet<string>(
            attributeNames.Select(Normalize),
            StringComparer.OrdinalIgnoreCase);

        bool IsTargetAttr(string name)
            => !string.IsNullOrEmpty(name) && nameSet.Contains(Normalize(name));

        // 递归化简：只处理 ConditionalExpression（AND/OR/比较）
        ConditionalExpression Simplify(ConditionalExpression expr)
        {
            switch (expr)
            {
                case null:
                    return null;

                // 如果是 NetworkAttributeComparison，命中目标属性则移除（返回 null）
                case NetworkAttributeComparison nac:
                    if (IsTargetAttr(nac.NetworkAttribute?.Name) ||
                        IsTargetAttr(nac.OtherNetworkAttribute?.Name))
                        return null;
                    return nac;

                // CategoryComparison 保留
                case CategoryComparison cc:
                    return cc;

                // AND：两边递归化简；有一边为空则“提升”另一边；都空则整棵删除
                case And and:
                    {
                        var left = Simplify(and.LeftExpression);
                        var right = Simplify(and.RightExpression);
                        if (left == null && right == null) return null;
                        if (left == null) return right;
                        if (right == null) return left;
                        return new And(left, right);
                    }

                // OR：逻辑与 AND 同理
                case Or or:
                    {
                        var left = Simplify(or.LeftExpression);
                        var right = Simplify(or.RightExpression);
                        if (left == null && right == null) return null;
                        if (left == null) return right;
                        if (right == null) return left;
                        return new Or(left, right);
                    }

                // 其他未知表达式类型：保持原样（通常不会出现）
                default:
                    return expr;
            }
        }

        // 入口：Barriers 是 Condition，运行时一般是 ConditionalExpression
        return (Condition)Simplify(barriers as ConditionalExpression);
    }
}
