﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Pihrtsoft.Snippets;
using Snippetica.CodeGeneration.Commands;

namespace Snippetica.CodeGeneration
{
    public abstract class SnippetGenerator
    {
        public IEnumerable<Snippet> GenerateSnippets(string sourceDirectoryPath, SearchOption searchOption = SearchOption.AllDirectories)
        {
            return SnippetSerializer.Deserialize(sourceDirectoryPath, searchOption)
                .SelectMany(snippet => GenerateSnippets(snippet));
        }

        public IEnumerable<Snippet> GenerateSnippets(Snippet snippet)
        {
            Debug.Assert(snippet.Language == Language.Html || snippet.Keywords.Any(f => f.StartsWith(KnownTags.MetaPrefix + KnownTags.GeneratePrefix, StringComparison.OrdinalIgnoreCase)), snippet.FilePath);

            foreach (Command command in CreateCommands(snippet))
            {
                ExecutionContext context = CreateExecutionContext(snippet);

                command.Execute(context);

                if (context.IsCanceled)
                    continue;

                Collection<Snippet> snippets = context.Snippets;

                for (int i = 0; i < snippets.Count; i++)
                    yield return PostProcess(snippets[i]);
            }
        }

        protected virtual ExecutionContext CreateExecutionContext(Snippet snippet)
        {
            return new ExecutionContext((Snippet)snippet.Clone());
        }

        protected abstract MultiCommandCollection CreateCommands(Snippet snippet);

        protected virtual Snippet PostProcess(Snippet snippet)
        {
            snippet.AddTag(KnownTags.AutoGenerated);

            return snippet;
        }

        public static IEnumerable<Snippet> GenerateAlternativeShortcuts(List<Snippet> snippets)
        {
            int count = snippets.Count;

            for (int i = 0; i < count; i++)
            {
                if (snippets[i].TryGetTag(KnownTags.AlternativeShortcut, out TagInfo info))
                {
                    snippets[i].Keywords.RemoveAt(info.KeywordIndex);

                    yield return GenerateSnippet(snippets[i], info.Value);
                }
            }

            Snippet GenerateSnippet(Snippet snippet, string shortcut)
            {
                snippet = (Snippet)snippet.Clone();

                snippet.Shortcut = shortcut;
                snippet.SuffixTitle(" _");
                snippet.AddTag(KnownTags.TitleEndsWithUnderscore);
                snippet.SuffixFileName("_");

                return snippet;
            }
        }
    }
}