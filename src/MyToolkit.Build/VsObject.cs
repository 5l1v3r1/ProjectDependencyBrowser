﻿//-----------------------------------------------------------------------
// <copyright file="VsObject.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/MyToolkit/MyToolkit/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace MyToolkit.Build
{
    /// <summary>Describes a Visual Studio object. </summary>
    public abstract class VsObject
    {
        /// <summary>Initializes a new instance of the <see cref="VsObject"/> class. </summary>
        /// <param name="path">The path to the object. </param>
        protected VsObject(string path)
        {
            Id = GetIdFromPath(path);
            Path = path;
        }

        /// <summary>Gets the id of the object. </summary>
        public string Id { get; private set; }

        /// <summary>Gets the path of the project file. </summary>
        public string Path { get; private set; }

        /// <summary>Gets the name of the project. </summary>
        public abstract string Name { get; }

        /// <summary>Gets the file name of the project. </summary>
        public string FileName
        {
            get { return System.IO.Path.GetFileName(Path); }
        }

        /// <summary>Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>. </summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false. </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            if (!(obj is VsObject))
                return false;

            return Id == ((VsObject)obj).Id;
        }

        /// <summary>Serves as a hash function for a particular type. </summary>
        /// <returns>A hash code for the current <see cref="T:System.Object"/>. </returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        internal static string GetIdFromPath(string path)
        {
            return System.IO.Path.GetFullPath(path).ToLower();
        }

        internal static Task<List<T>> LoadAllFromDirectoryAsync<T>(string path, string includedPathFilter, string excludedPathFilter, bool ignoreExceptions, ProjectCollection projectCollection, string extension, Func<string, ProjectCollection, T> creator, Dictionary<string, Exception> errors)
        {
            var includedPathFilterTerms = includedPathFilter.ToLowerInvariant().Split(' ').Where(t => !string.IsNullOrEmpty(t)).ToArray();
            var excludedPathFilterTerms = excludedPathFilter.ToLowerInvariant().Split(' ').Where(t => !string.IsNullOrEmpty(t)).ToArray();

            return Task.Run(async () =>
            {
                var tasks = new List<Task<T>>();
                var projects = new List<T>();

                try
                {
                    var files = GetFiles(path, "*" + extension);
                    foreach (var file in files.Distinct().Where(s => 
                        includedPathFilterTerms.All(t => s.ToLowerInvariant().Contains(t)) && 
                        excludedPathFilterTerms.All(t => !s.ToLowerInvariant().Contains(t))))
                    {
                        var ext = System.IO.Path.GetExtension(file);
                        if (ext != null && ext.ToLower() == extension)
                        {
                            var lFile = file;
                            tasks.Add(Task.Run(() =>
                            {
                                try
                                {
                                    return creator(lFile, projectCollection);
                                }
                                catch (Exception exception)
                                {
                                    if (!ignoreExceptions)
                                        throw;

                                    if (errors != null)
                                        errors[lFile] = exception;
                                }
                                return default(T);
                            }));
                        }
                    }

                    await Task.WhenAll(tasks);

                    foreach (var task in tasks.Where(t => t.Result != null))
                        projects.Add(task.Result);
                }
                catch (Exception exception)
                {
                    if (!ignoreExceptions)
                        throw;

                    if (errors != null)
                        errors[path] = exception;
                }

                return projects;
            });
        }

        private static List<string> GetFiles(string path, string pattern)
        {
            var files = new List<string>();

            if (path.EndsWith("$Recycle.Bin"))
                return files;

            if (path.ToLowerInvariant().Contains("\\temp\\"))
                return files;

            if (path == Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
                return files;

            if (path == Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86))
                return files;

            try
            {
                files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                foreach (var directory in Directory.GetDirectories(path))
                    files.AddRange(GetFiles(directory, pattern));
            }
            catch (UnauthorizedAccessException) { }

            return files;
        }
    }
}
