# cloudscribe SimpleContent

A simple, yet flexible content and blog engine for ASP.NET Core that can work with or without a database. This project has borrowed significantly from [Mads Kristensen's MiniBlog](https://github.com/madskristensen/MiniBlog) both for ideas and code but re-implemented and extended in the newer ASP.NET Core framework. 

This project supports content pages in addition to blog posts, you can create and edit both pages and blog posts in the web browser or using [Open Live Writer](https://github.com/OpenLiveWriter/OpenLiveWriter) via the MetaWeblog API. I created a separate project, [cloudscribe.MetaWeblog](https://github.com/joeaudette/cloudscribe.MetaWeblog), in order to support using Open Live Writer, but it could be used in other apps as well, so I moved it to its own code repository.

If you have questions or just want to be social, say hello in our gitter chat room. I try to monitor that room on a regular basis while I'm working, but if I'm not around you can leave  message.

[![Join the chat at https://gitter.im/joeaudette/cloudscribe](https://badges.gitter.im/joeaudette/cloudscribe.svg)](https://gitter.im/joeaudette/cloudscribe?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## Current Status

This project is in fairly early stages, lots of things will change or be refactored but it is starting to shape up nicely and I thought I should go ahead and make the project public.

There are no releases or nugets yet, but you can try it in visual studio. If you launch it with IIS Express it will have a single tenant running at localhost:53941, change to port-tenants in the debugging dropdown (instead of IISExpress), you can debug with 2 tenants running. You can also run both configured tenants by opening a command window in the root of the example.WebApp project and execute the command:

    dotnet run
	
that will get you both localhost:60000 and localhost:60002 which you can open in your web browser. Both sites are pre-configured with some content and you can login with admin/admin in either or both tenants.

After you login a little pencil will appear in the upper right corner, it toggles the editor toolbar.

You should also be able to [create and edit blog posts and pages using Open Live Writer](https://github.com/joeaudette/cloudscribe.SimpleContent/wiki/Using-Open-Live-Writer)

## Start simple with no database and migrate to a database later if you need one

Not all web site projects need a database, there can be many benefits to not using one including performance, scalability, portability, lower cost, and ease of making backup copies of the entire site. It should even be possible to make a site that runs from a thumb drive.

In fact, for blogs, there has been kind of a trend towards using [Static Site Generators](https://www.staticgen.com/). This project is not a static site generator, but by storing content as json files it can get some of the same benefits and be used in a similar way to using a static site generator. For example you could host a localhost or intranet version of your site for producing and reviewing content, then when ready to publish you could commit the changes to a git repository and then do deployment from git to Azure for example, which would give you a highly scaleable site without the need or cost of a database and with a complete history of changes in git. Personal blogs and sites and small brochure sites are good candidates for not using a database.

Some sites do need a database though and we plan to support using both Entity Framework Core and MongoDb. If you need users to be able to register on your site or if you have more than a few editors, or for larger projects, you will typically want a database.

My plan is to usually build sites without a database (except for large projects), but implement a migration utility to be able to migrate any site from files to a database later if the needs of the project require it.

### Current Features
* Cross platform, works on Windows, OSX, and Linux
* __No database required__ uses json for pages and can use json or xml for blog posts via [NoDb](https://github.com/joeaudette/NoDb)
* For blog posts, supports the same XML format as MiniBlog and BlogEngine.NET, to convert from one of those, just drop in your files
* Migrate your existing blog to SimpleContent using [MiniBlog Formatter](https://github.com/madskristensen/MiniBlogFormatter)
* __Inline editing__ of blog posts and pages
* Supports multiple tenants by host name even without a database
* Easy setting for serving static files from another domain. 
*  __Open Live Writer__ (OLW) and __Windows Live Writer__ (WLW) support
* You don't have to use OLW/WLW (but you should)
* Schedule posts to be published on a future date
* Supports blog urls with or without date segments
* Url date segments are hackable, ie /blog/2016/03/16 shows posts for the day, /blog/2016/03 shows posts for the month and /blog/2016 shows posts for the year
* Comments support - can easily be replaced by 3rd-party commenting systems such as Disqus
* [Recaptcha support](https://www.google.com/recaptcha/intro/index.html) to reduce comment spam
* __Gravatar__ support 
* __Drag and drop images__ to upload
* Responsive theming support based on Bootstrap
* Uses HTML 5 __microdata__ to add semantic meaning
* Works on any ASP.NET Core host including __Windows Azure__ Websites
* RSS feed built in at /api/rss via [cloudscribe.Syndication project](https://github.com/joeaudette/cloudscribe.Syndication) 
* Sitemap - built in [sitemap](http://www.sitemaps.org/schemas/sitemap/0.9) at /api/sitemap via [cloudscribe.Web.SiteMap](https://github.com/joeaudette/cloudscribe.Web.Navigation/tree/master/src/cloudscribe.Web.SiteMap) 

### Planned Features - see also the to-do.md in the notes folder
* Support for automatic image resizing and optimization - pending progress on [ImageProcessor project](https://github.com/JimBobSquarePants/ImageProcessor). This is needed for images added via the wysiwyg editor in the web browser. The best current solution is to use Open Live Writer which will resize the image before it posts to the server, or optimize the images yourself before adding them to content in the wysiwyg editor in the web browser.
* Support for using Entity Framework Core
* Support for using cloudscribe.Core for user and site/tenant management
* Support for using MongoDb
* A Utility for importing the json or xml content into Entity Framework Core or MongoDb for easy migration
* Automatically __optimizes uploaded images__


