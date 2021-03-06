﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:                  Joe Audette
// Created:                 2016-02-09
// Last Modified:           2016-07-13
// 

using cloudscribe.SimpleContent.Common;
using cloudscribe.SimpleContent.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace cloudscribe.SimpleContent.Services
{
    public class BlogService : IBlogService
    {
        public BlogService(
            IProjectService projectService,
            IProjectSecurityResolver security,
            IPostRepository blogRepository,
            IMediaProcessor mediaProcessor,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccesor,
            IHttpContextAccessor contextAccessor = null)
        {
            this.security = security;
            repo = blogRepository;
            context = contextAccessor?.HttpContext;
            this.mediaProcessor = mediaProcessor;
            this.urlHelperFactory = urlHelperFactory;
            this.actionContextAccesor = actionContextAccesor;
            this.projectService = projectService;
            htmlProcessor = new HtmlProcessor();
        }

        private IProjectService projectService;
        private IProjectSecurityResolver security;
        private readonly HttpContext context;
        private CancellationToken CancellationToken => context?.RequestAborted ?? CancellationToken.None;
        private IUrlHelperFactory urlHelperFactory;
        private IActionContextAccessor actionContextAccesor;
        private IPostRepository repo;
        private IMediaProcessor mediaProcessor;
        private ProjectSettings settings = null;
        private bool userIsBlogOwner = false;
        private HtmlProcessor htmlProcessor;

        private async Task<bool> EnsureBlogSettings()
        {
            if(settings != null) { return true; }
            settings = await projectService.GetCurrentProjectSettings().ConfigureAwait(false);
            if (settings != null)
            {
                if(context.User.Identity.IsAuthenticated)
                {
                    var userBlog = context.User.GetProjectId();
                    if(!string.IsNullOrEmpty(userBlog))
                    {
                        if(settings.ProjectId == userBlog) { userIsBlogOwner = true; }

                    }
                }

                return true;
            }
            return false;
        }

        //public async Task<ProjectSettings> GetCurrentBlogSettings()
        //{
        //    await EnsureBlogSettings().ConfigureAwait(false);
        //    return settings;
        //}

        //public async Task<List<ProjectSettings>> GetUserProjects(string userName)
        //{
        //    //await EnsureBlogSettings().ConfigureAwait(false);
        //    //return settings;
        //    return await projectService.GetUserProjects(userName).ConfigureAwait(false);
        //}

        //public async Task<ProjectSettings> GetProjectSettings(string projectId)
        //{
        //    //await EnsureBlogSettings().ConfigureAwait(false);
        //    //return settings;
        //    return await projectService.GetProjectSettings(projectId).ConfigureAwait(false);
        //}

        //public async Task<List<Post>> GetAllPosts()
        //{
        //    await EnsureBlogSettings().ConfigureAwait(false);

        //    return await repo.GetAllPosts(settings.BlogId, CancellationToken).ConfigureAwait(false);
        //}

        public async Task<List<Post>> GetVisiblePosts()
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetVisiblePosts(
                settings.ProjectId,
                userIsBlogOwner,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<PagedResult<Post>> GetVisiblePosts(
            string category,
            int pageNumber)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetVisiblePosts(
                settings.ProjectId,
                category,
                userIsBlogOwner,
                pageNumber,
                settings.PostsPerPage,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<int> GetCount(string category)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetCount(
                settings.ProjectId,
                category,
                userIsBlogOwner,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<int> GetCount(
            string projectId,
            int year,
            int month = 0,
            int day = 0)
        {
            return await repo.GetCount(
                projectId,
                year,
                month,
                day,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<Post>> GetRecentPosts(int numberToGet)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetRecentPosts(
                settings.ProjectId,
                numberToGet,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<Post>> GetRecentPosts(
            string projectId, 
            string userName,
            string password,
            int numberToGet)
        {
            var permission = await security.ValidatePermissions(
                projectId,
                userName,
                password,
                CancellationToken
                ).ConfigureAwait(false);

            if(!permission.CanEdit)
            {
                return new List<Post>(); // empty
            }

            
            return await repo.GetRecentPosts(
                projectId,
                numberToGet,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<PagedResult<Post>> GetPosts(
            string projectId, 
            int year, 
            int month = 0, 
            int day = 0, 
            int pageNumber = 1, 
            int pageSize = 10)
        {
            return await repo.GetPosts(projectId, year, month, day, pageNumber, pageSize).ConfigureAwait(false);
        }

        public async Task Save(
            string projectId, 
            string userName,
            string password,
            Post post, 
            bool isNew, 
            bool publish)
        {
            var permission = await security.ValidatePermissions(
                projectId,
                userName,
                password,
                CancellationToken
                ).ConfigureAwait(false);

            if (!permission.CanEdit)
            {
                return; 
            }

            var settings = await projectService.GetProjectSettings(projectId).ConfigureAwait(false);

            if(isNew)
            {
                await InitializeNewPosts(projectId, post, publish);
            }


            //contextAccessor
            var urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccesor.ActionContext);
            var imageAbsoluteBaseUrl = urlHelper.Content("~" + settings.LocalMediaVirtualPath);
            if(context != null)
            {
                imageAbsoluteBaseUrl = context.Request.AppBaseUrl() + settings.LocalMediaVirtualPath;
            }

            // open live writer passes in posts with absolute urls
            // we want to change them to relative to keep the files portable
            // to a different root url
            post.Content = await htmlProcessor.ConvertMediaUrlsToRelative(
                settings.LocalMediaVirtualPath,
                imageAbsoluteBaseUrl, //this shold be resolved from virtual using urlhelper
                post.Content);

            // here we need to process any base64 embedded images
            // save them under wwwroot
            // and update the src in the post with the new url
            // since this overload of Save is only called from metaweblog
            // and metaweblog does not base64 encode the images like the browser client
            // this call may not be needed here
            //await mediaProcessor.ConvertBase64EmbeddedImagesToFilesWithUrls(
            //    settings.LocalMediaVirtualPath,
            //    post
            //    ).ConfigureAwait(false);

            var nonPublishedDate = new DateTime(1, 1, 1);
            if (post.PubDate == nonPublishedDate)
            {
                post.PubDate = DateTime.UtcNow;
            }

            await repo.Save(settings.ProjectId, post, isNew).ConfigureAwait(false);
        }

        public async Task Save(Post post, bool isNew)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            // here we need to process any base64 embedded images
            // save them under wwwroot
            // and update the src in the post with the new url
            await mediaProcessor.ConvertBase64EmbeddedImagesToFilesWithUrls(
                settings.LocalMediaVirtualPath,
                post
                ).ConfigureAwait(false);

            var nonPublishedDate = new DateTime(1, 1, 1);
            if(post.PubDate == nonPublishedDate)
            {
                post.PubDate = DateTime.UtcNow;
            }

            await repo.Save(settings.ProjectId, post, isNew).ConfigureAwait(false);
        }

        public async Task HandlePubDateAboutToChange(Post post, DateTime newPubDate)
        {
            await repo.HandlePubDateAboutToChange(post, newPubDate);
        }

        private async Task InitializeNewPosts(string projectId, Post post, bool publish)
        {
            if(publish)
            {
                post.PubDate = DateTime.UtcNow;
            }

            if(string.IsNullOrEmpty(post.Slug))
            {
                var slug = CreateSlug(post.Title);
                var available = await SlugIsAvailable(slug);
                if (available)
                {
                    post.Slug = slug;
                }

            }
        }

        public async Task<string> ResolveMediaUrl(string fileName)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return settings.LocalMediaVirtualPath + fileName;
        }

        public async Task<string> ResolvePostUrl(Post post)
        {
            await EnsureBlogSettings().ConfigureAwait(false);
            var urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccesor.ActionContext);
            string postUrl;
            if (settings.IncludePubDateInPostUrls)
            {
                postUrl = urlHelper.RouteUrl(ProjectConstants.PostWithDateRouteName,
                    new
                    {
                        year = post.PubDate.Year,
                        month = post.PubDate.Month.ToString("00"),
                        day = post.PubDate.Day.ToString("00"),
                        slug = post.Slug
                    });
            }
            else
            {
                postUrl = urlHelper.RouteUrl(ProjectConstants.PostWithoutDateRouteName,
                    new { slug = post.Slug });
            }

            return postUrl;
            //var result = urlHelper.Action("Post", "Blog", new { slug = post.Slug });

            //return result;
        }

        

        public Task<string> ResolveBlogUrl(ProjectSettings blog)
        {
            //await EnsureBlogSettings().ConfigureAwait(false);

            var urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccesor.ActionContext);
            var result = urlHelper.Action("Index", "Blog");

            return Task.FromResult(result);
        }


        public async Task<Post> GetPost(string postId)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetPost(
                settings.ProjectId,
                postId,
                CancellationToken)
                .ConfigureAwait(false);

        }

        public async Task<Post> GetPost(
            string projectId, 
            string postId,
            string userName,
            string password
            )
        {

            var permission = await security.ValidatePermissions(
                projectId,
                userName,
                password,
                CancellationToken
                ).ConfigureAwait(false);

            if (!permission.CanEdit)
            {
                return null;
            }
            // await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetPost(
                projectId,
                postId,
                CancellationToken)
                .ConfigureAwait(false);

        }

        public async Task<Post> GetPostBySlug(string slug)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetPostBySlug(
                settings.ProjectId,
                slug,
                CancellationToken)
                .ConfigureAwait(false);

        }

        public string CreateSlug(string title)
        {
            return ContentUtils.CreateSlug(title);
        }

        public async Task<bool> SlugIsAvailable(string slug)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.SlugIsAvailable(
                settings.ProjectId,
                slug,
                CancellationToken)
                .ConfigureAwait(false);
        }
        
        public async Task<bool> SlugIsAvailable(string projectId, string slug)
        {
            

            return await repo.SlugIsAvailable(
                projectId,
                slug,
                CancellationToken)
                .ConfigureAwait(false);
        }

        

        public async Task Delete(string postId)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            await repo.Delete(settings.ProjectId, postId).ConfigureAwait(false);

        }

        public async Task Delete(
            string projectId, 
            string postId,
            string userName,
            string password)
        {
            var permission = await security.ValidatePermissions(
                projectId,
                userName,
                password,
                CancellationToken
                ).ConfigureAwait(false);

            if (!permission.CanEdit)
            {
                return; //TODO: exception here?
            }
            //await EnsureBlogSettings().ConfigureAwait(false);
            //var settings = await settingsRepo.GetBlogSetings(projectId, CancellationToken).ConfigureAwait(false);

            await repo.Delete(projectId, postId).ConfigureAwait(false);

        }

        public async Task<Dictionary<string, int>> GetCategories()
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetCategories(
                settings.ProjectId,
                userIsBlogOwner,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<Dictionary<string, int>> GetCategories(
            string projectId, 
            string userName,
            string password)
        {
            var permission = await security.ValidatePermissions(
                projectId,
                userName,
                password,
                CancellationToken
                ).ConfigureAwait(false);

            if (!permission.CanEdit)
            {
                return new Dictionary<string, int>(); //empty
            }
            var settings = await projectService.GetProjectSettings(projectId).ConfigureAwait(false);

            return await repo.GetCategories(
                settings.ProjectId,
                permission.CanEdit,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<Dictionary<string, int>> GetArchives()
        {
            await EnsureBlogSettings().ConfigureAwait(false);
            //var settings = await projectService.GetProjectSettings(projectId).ConfigureAwait(false);

            return await repo.GetArchives(
                settings.ProjectId,
                userIsBlogOwner,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<bool> CommentsAreOpen(Post post, bool userIsOwner)
        {
            if(userIsBlogOwner) { return true; }
            await EnsureBlogSettings().ConfigureAwait(false);

            if(settings.DaysToComment == -1) { return true; }

            var result = post.PubDate > DateTime.UtcNow.AddDays(-settings.DaysToComment);
            return result;
        }

        public async Task SaveMedia(
            string projectId, 
            string userName,
            string password,
            byte[] bytes, string 
            fileName)
        {
            var permission = await security.ValidatePermissions(
                projectId,
                userName,
                password,
                CancellationToken
                ).ConfigureAwait(false);

            if (!permission.CanEdit)
            {
                return;
            }

            var settings = await projectService.GetProjectSettings(projectId).ConfigureAwait(false);

            await mediaProcessor.SaveMedia(settings.LocalMediaVirtualPath, fileName, bytes).ConfigureAwait(false);
        }

        
    }
}
