#region Using directives

using System;
using System.Net;
using System.Xml;
using System.Reflection;

using NGExtension;

#endregion

namespace GraemeF.NewsGator
{
    public partial class FetchLinks : INewsGatorExtension
    {
        /// <summary>
        /// Logger for the FetchLinks class.
        /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(FetchLinks));

        // object used as a mutex
        private static object mutex = new object();

        #region UserAgent property
        private static string userAgent;

        /// <summary>
        /// Gets a string used to identify the program to web servers (shows up in web server logs).
        /// </summary>
        protected static string UserAgent
        {
            get
            {
                // make sure the UserAgent string is only generated once across all threads
                if (userAgent == null)
                    lock (mutex)
                        if (userAgent == null)
                        {
                            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
                            userAgent = string.Format("{0} extension for NewsGator/{1} (http://graemef.com)", assemblyName.Name, assemblyName.Version.ToString());
                        }

                return userAgent;
            }
        }
        #endregion

        /// <summary>
        /// Gets the content for a post.
        /// </summary>
        /// <param name="feedUrl">URL of the feed the post came from.</param>
        /// <param name="url">The link from the post.</param>
        /// <returns>The content retrieved from the given link.</returns>
        private string GetContent(string feedUrl, string url)
        {
            WebRequest req = WebRequest.Create(url);

            // if the link points at a web server (http)
            if (req is HttpWebRequest)
            {
                HttpWebRequest httpReq = (HttpWebRequest)req;

                // tell the web server what this is and how we found the link
                httpReq.UserAgent = UserAgent;
                httpReq.Referer = feedUrl;
            }

            // send the request and get the response
            using (WebResponse response = req.GetResponse())
            {
                // decode the body of the response
                string content = HtmlUtil.DecodeData(response);

                // point any relative URI's in the content to the right place
                return HtmlUtil.SetBaseUri(content, response.ResponseUri.AbsoluteUri);
            }
        }

        /// <summary>
        /// The string to be replaced in the stylesheet.
        /// </summary>
        private string contentPlaceholder = "<div class=\"fetchlinks\" />";

        #region INewsGatorExtension Members

        #region Unused methods
        /// <summary>
        /// Called once, on one instance of your extension, when NewsGator is
        /// about to begin processing new items.
        /// </summary>
        public void BeginRetrieve()
        {
            using (log4net.NDC.Push("BeginRetrieve"))
                log.Info("No action.");
        }

        /// <summary>
        /// Called once, on the same instance of your extension that BeginRetrieve
        /// was called on, when NewsGator has completed retrieving new items.
        /// </summary>
        public void EndRetrieve()
        {
            using (log4net.NDC.Push("EndRetrieve"))
                log.Info("No action.");
        }

        /// <summary>
        /// Called when the PostItem in Outlook has been created (assuming 
        /// createPost from PreProcessItem was true).  
        /// 
        /// This function will be called from the Outlook GUI thread.  This means
        /// you may access the Outlook object model through the passed in objects;
        /// however, keep the amount of processing done in this function to an
        /// absolute minimum, to preserve the user experience.
        /// </summary>
        /// <param name="reference">
        /// Reference object that the extension returned from PreProcessItem.
        /// </param>
        /// <param name="postItem">
        /// This will be the just-created PostItem object in Outlook.  If you wish
        /// to manipulate this object, add a reference to the Outlook XP Primary
        /// Interop Assembly, and cast the object to a PostItem.
        /// 
        /// If PreProcessItem returned createPost=false, then the postItem 
        /// parameter will be null.
        /// </param>
        /// <param name="appObj">
        /// The Outlook application object, which you can use to manipulate other
        /// parts of the Outlook object model.  If you wish
        /// to manipulate this object, add a reference to the Outlook XP Primary
        /// Interop Assembly, and cast the object to a Application.
        /// </param>
        public void PostProcessItem(object reference, object postItem, object appObj)
        {
            using (log4net.NDC.Push("PostProcessItem"))
                log.Info("No action.");
        }
        #endregion

        /// <summary>
        /// Called while an item is being processed. If this function is called,
        /// NewsGator is processing an item that appears to be "new", and the 
        /// process of adding it to Outlook has been started.
        /// 
        /// This function will be called on one of the NewsGator retrieval threads.
        /// There are 1-5 of these threads, controlled by the "performance" setting
        /// in NewsGator/Options, Options tab.  You MUST NOT access the Outlook
        /// object model during this function.
        /// 
        /// Instance management is subject to change.  This means that NewsGator
        /// may call PreProcessItem 3 times, then PostProcessItem 3 times, or it
        /// may interleave the calls.  Use the reference object returned by 
        /// PreProcessItem to correlate Pre- and Post- calls. 
        /// </summary>
        /// <param name="postInfo">
        /// This is the normalized information NewsGator has parsed from the RSS item.
        /// This information will be sent to the code that actually creates the 
        /// PostItem in Outlook; any changes you make to this object will be 
        /// carried forward and will affect the Outlook PostItem.
        /// </param>
        /// <param name="originalItem">
        /// This is the actual RSS item XML for the item being processed.  If your
        /// extension will process custom extensions, for example, you will find
        /// them here.  The node element will be named originalItem, and the actual
        /// RSS item will be the first child of this element.
        /// </param>
        /// <param name="createPost">
        /// Output parameter, defines whether or not the post should be created in
        /// Outlook.  If you return with this parameter set to true, PostProcessItem
        /// will still be called, but its postItem parameter will be null.
        /// </param>
        /// <returns>
        /// Returns a reference object which will be passed to PostProcessItem as the
        /// reference parameter.
        /// </returns>
        public object PreProcessItem(PostInfo postInfo, XmlNode originalItem, out bool createPost)
        {
            using (log4net.NDC.Push("PreProcessItem"))
            {
                // only do anything if the stylesheet contains the placeholder
                if (postInfo.Description.IndexOf(contentPlaceholder) >= 0)
                {
                    string content = string.Empty;

                    // need a link otherwise we don't know whatto retrieve!
                    if (postInfo.PostLink != string.Empty && postInfo.PostLink != null)
                    {
                        try
                        {
                            content = GetContent(postInfo.FromAddr, postInfo.PostLink);
                        }
                        // I know we should *never* catch all exceptions, but we really don't want to take Outlook out
                        catch (Exception ex)
                        {
                            log.Error("Failed to get content from " + postInfo.PostLink, ex);
                        }
                    }

                    // replace the placeholder in the post with the content
                    postInfo.Description = postInfo.Description.Replace(contentPlaceholder, content);
                }

                // all is well
                createPost = true;
                return postInfo;
            }
        }

        #endregion
    }
}
