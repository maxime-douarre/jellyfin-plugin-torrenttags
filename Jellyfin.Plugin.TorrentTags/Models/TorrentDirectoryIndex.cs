using System;
using System.Collections.Generic;
using System.IO;

namespace Jellyfin.Plugin.TorrentTags.Models;

public class TorrentDirectoryIndex
{
    private readonly Dictionary<string, TorrentDirectoryIndexNode> _children;

    public TorrentDirectoryIndex()
    {
        _children = [];
    }

    private static TorrentDirectoryBranchNode BuildTorrentBranch(DirectoryInfo? parentDirectory, TorrentDirectoryBranchNode childNode)
    {
        if (parentDirectory is null)
            return childNode;

        TorrentDirectoryBranchNode parentNode = new(
            Name: parentDirectory.Name,
            Child: childNode,
            Tags: []);

        return BuildTorrentBranch(parentDirectory.Parent, parentNode);
    }

    private static void MergeTorrentBranch(TorrentDirectoryBranchNode branchNode, Dictionary<string, TorrentDirectoryIndexNode> indexNodes)
    {
        if (indexNodes.TryGetValue(branchNode.Name, out TorrentDirectoryIndexNode? indexNode))
        {
            indexNode.Tags.UnionWith(branchNode.Tags);
        }
        else
        {
            indexNode = new TorrentDirectoryIndexNode(
                Children: [],
                Tags: branchNode.Tags);

            indexNodes[branchNode.Name] = indexNode;
        }

        if (branchNode.Child is not null)
            MergeTorrentBranch(branchNode.Child, indexNode.Children);
    }

    public void AddTorrent(DirectoryInfo contentPath, HashSet<string> tags)
    {
        ArgumentNullException.ThrowIfNull(contentPath);
        ArgumentNullException.ThrowIfNull(tags);

        TorrentDirectoryBranchNode branch = new(
            Name: contentPath.Name,
            Child: null,
            Tags: tags);

        branch = BuildTorrentBranch(contentPath.Parent, branch);

        MergeTorrentBranch(branch, _children);
    }

    private static BaseItemDirectoryBranchNode BuildBaseItemBranch(DirectoryInfo? parentDirectory, BaseItemDirectoryBranchNode childNode)
    {
        if (parentDirectory is null)
            return childNode;

        BaseItemDirectoryBranchNode parentNode = new(
            Name: parentDirectory.Name,
            Child: childNode);

        return BuildBaseItemBranch(parentDirectory.Parent, parentNode);
    }

    private static void CollectAllTags(HashSet<string> collectedTags, IReadOnlyCollection<TorrentDirectoryIndexNode> indexNodes)
    {
        foreach (TorrentDirectoryIndexNode indexNode in indexNodes)
        {
            collectedTags.UnionWith(indexNode.Tags);
            CollectAllTags(collectedTags, indexNode.Children.Values);
        }
    }

    private static void CollectBranchTags(HashSet<string> collectedTags, BaseItemDirectoryBranchNode? branchNode, Dictionary<string, TorrentDirectoryIndexNode> indexNodes)
    {
        if (branchNode is null)
        {
            CollectAllTags(collectedTags, indexNodes.Values);
        }
        else if (indexNodes.TryGetValue(branchNode.Name, out TorrentDirectoryIndexNode? indexNode))
        {
            collectedTags.UnionWith(indexNode.Tags);
            CollectBranchTags(collectedTags, branchNode.Child, indexNode.Children);
        }
    }

    public HashSet<string> GetTags(DirectoryInfo baseItemPath)
    {
        ArgumentNullException.ThrowIfNull(baseItemPath);

        BaseItemDirectoryBranchNode baseItemDirectoryBranchNode = new(
            Name: baseItemPath.Name,
            Child: null);

        baseItemDirectoryBranchNode = BuildBaseItemBranch(baseItemPath.Parent, baseItemDirectoryBranchNode);

        HashSet<string> collectedTags = [];
        CollectBranchTags(collectedTags, baseItemDirectoryBranchNode, _children);
        return collectedTags;
    }

    private record TorrentDirectoryIndexNode(
        Dictionary<string, TorrentDirectoryIndexNode> Children,
        HashSet<string> Tags);

    private record TorrentDirectoryBranchNode(
        string Name,
        TorrentDirectoryBranchNode? Child,
        HashSet<string> Tags);

    private record BaseItemDirectoryBranchNode(
        string Name,
        BaseItemDirectoryBranchNode? Child);
}
