﻿/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Components.HTMLTopicPages.Class
 * ____________________________________________________________________________
 * 
 * Creates a <HTMLTopicPage> for a class.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Topics;
using GregValure.NaturalDocs.Engine.TopicTypes;


namespace GregValure.NaturalDocs.Engine.Output.Components.HTMLTopicPages
	{
	public class Class : HTMLTopicPage
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Class
		 * 
		 * Creates a new class topic page.  Which features are available depend on which parameters you pass.
		 * 
		 * - If you pass a <ClassString> AND a class ID, all features are available immediately.
		 * - If you only pass a <ClassString>, then only the <path properties> are available.
		 * - If you only pass a class ID, then only the <database functions> are available.
		 *		- The <path properties> will be available after one of the <database functions> is called as they will look up the
		 *			<ClassString>.
		 *		- It is safe to call <HTMLTopicPage.Build()> when you only passed a class ID.
		 */
		public Class (Builders.HTML htmlBuilder, int classID = 0, ClassString classString = default(ClassString)) : base (htmlBuilder)
			{
			// DEPENDENCY: This class assumes HTMLTopicPage.Build() will call a database function before using any path properties.

			this.classID = classID;
			this.classString = classString;
			}



		// Group: Database Functions
		// __________________________________________________________________________


		/* Function: GetTopics
		 * 
		 * Retrieves the <Topics> in the class.  If there are no topics it will return an empty list.
		 * 
		 * If the <CodeDB.Accessor> doesn't have a lock this function will acquire and release a read-only lock.
		 * If it already has a lock it will use it and not release it.
		 */
		public override List<Topic> GetTopics (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (classID <= 0)
				{  throw new Exception("You cannot use HTMLTopicPages.Class.GetTopics when classID is not set.");  }
			#endif


			// Retrieve the topics from the database.

			bool releaseLock = false;
			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{
				accessor.GetReadOnlyLock();
				releaseLock = true;
				}

			List<Topic> topics = null;
			
			try
				{  
				if (classString == null)
					{  classString = accessor.GetClassByID(classID);  }

				topics = accessor.GetTopicsInClass(classID, cancelDelegate);  
				}
			finally
				{
				if (releaseLock)
					{  accessor.ReleaseLock();  }
				}


			// Filter out any list topics that are members of the hierarchy.  If someone documents classes as part of a list,
			// we only want pages for the individual members, not the list topic.

			for (int i = 0; i < topics.Count; i++)
				{
				TopicType topicType = Engine.Instance.TopicTypes.FromID(topics[i].TopicTypeID);
				bool remove = false;

				if (topicType.Flags.ClassHierarchy || topicType.Flags.DatabaseHierarchy)
					{
					if (topics[i].Body != null && topics[i].Body.IndexOf("<ds") != -1)
						{  remove = true;  }
					}

				if (remove)
					{  topics.RemoveAt(i);  }
				else
					{  i++;  }
				}


			// Merge the topics from multiple files into one coherent list.

			MergeTopics(topics);


			return topics;
			}


		/* Function: GetLinks
		 * 
		 * Retrieves the <Links> appearing in the class.  If there are no links it will return an empty list.
		 * 
		 * If the <CodeDB.Accessor> doesn't have a lock this function will acquire and release a read-only lock.
		 * If it already has a lock it will use it and not release it.
		 */
		public override List<Link> GetLinks (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (classID <= 0)
				{  throw new Exception("You cannot use HTMLTopicPages.Class.GetLinks when classID is not set.");  }
			#endif

			bool releaseLock = false;
			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{
				accessor.GetReadOnlyLock();
				releaseLock = true;
				}

			List<Link> links = null;

			try
				{  
				if (classString == null)
					{  classString = accessor.GetClassByID(classID);  }

				links = accessor.GetLinksInClass(classID, cancelDelegate);  
				}
			finally
				{
				if (releaseLock)
					{  accessor.ReleaseLock();  }
				}

			return links;
			}


		/* Function: GetLinkTarget
		 */
		public override HTMLTopicPage GetLinkTarget (Topic targetTopic)
			{
			// We want to stay in the class view if we can, but since there's no generated page for globals we have to
			// switch to the file view for them.

			if (targetTopic.ClassID != 0)
				{  return new HTMLTopicPages.Class (htmlBuilder, targetTopic.ClassID, targetTopic.ClassString);  }
			else
				{  return new HTMLTopicPages.File (htmlBuilder, targetTopic.FileID);  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: PageTitle
		 */
		override public string PageTitle
			{
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use HTMLTopicPages.Class.PageTitle when classString is not set.");  }
				#endif

				return classString.Symbol.LastSegment;  
				}
			}

		/* Property: IncludeClassInTopicHashPaths
		 */
		override public bool IncludeClassInTopicHashPaths
			{
			get
				{  return false;  }
			}



		// Group: Path Properties
		// __________________________________________________________________________


		/* Property: OutputFile
		 * The path of the topic page's output file.
		 */
		override public Path OutputFile
		   {  
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				if (classString.Hierarchy == ClassString.HierarchyType.Class)
					{
					var language = Engine.Instance.Languages.FromID(classString.LanguageID);
					return htmlBuilder.Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + OutputFileNameOnly;
					}
				else // Database
					{  
					return htmlBuilder.Database_OutputFolder(classString.Symbol.WithoutLastSegment) + '/' + OutputFileNameOnly;
					}
				}
			}

		/* Property: OutputFileHashPath
		 * The hash path of the topic page.
		 */
		override public string OutputFileHashPath
			{
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				if (classString.Hierarchy == ClassString.HierarchyType.Class)
					{
					var language = Engine.Instance.Languages.FromID(classString.LanguageID);

					// OutputFolderHashPath already includes the trailing separator so we can just concatenate them.
					return htmlBuilder.Class_OutputFolderHashPath(language, classString.Symbol.WithoutLastSegment) + 
								 OutputFileNameOnlyHashPath;
					}
				else // Database
					{
					return htmlBuilder.Database_OutputFolderHashPath(classString.Symbol.WithoutLastSegment) + 
								 OutputFileNameOnlyHashPath;
					}
				}
			}

		/* Property: OutputFileNameOnly
		 * The output file name of topic page without the path.
		 */
		public Path OutputFileNameOnly
			{
			get
				{
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				string nameString = classString.Symbol.LastSegment;
				return Builders.HTML.SanitizePath(nameString, true) + ".html";
				}
			}


		/* Property: OutputFileNameOnlyHashPath
		 * The file name portion of the topic page's hash path.
		 */
		public string OutputFileNameOnlyHashPath
			{
			get
				{
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				string nameString = classString.Symbol.LastSegment;
				return Builders.HTML.SanitizePath(nameString);
				}
			}

		/* Property: ToolTipsFile
		 * The path of the topic page's tool tips file.
		 */
		override public Path ToolTipsFile
		   {  
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				if (classString.Hierarchy == ClassString.HierarchyType.Class)
					{
					var language = Engine.Instance.Languages.FromID(classString.LanguageID);
					return htmlBuilder.Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + ToolTipsFileNameOnly;
					}
				else // Database
					{
					return htmlBuilder.Database_OutputFolder(classString.Symbol.WithoutLastSegment) + '/' + ToolTipsFileNameOnly;
					}
				}
			}

		/* Property: ToolTipsFileNameOnly
		 * The file name of the topic page's tool tips file without the path.
		 */
		public Path ToolTipsFileNameOnly
			{
			get
				{
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				string nameString = classString.Symbol.LastSegment;
				return Builders.HTML.SanitizePath(nameString, true) + "-ToolTips.js";
				}
			}

		/* Property: SummaryFile
		 * The path of the topic page's summary file.
		 */
		override public Path SummaryFile
		   {  
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				if (classString.Hierarchy == ClassString.HierarchyType.Class)
					{
					var language = Engine.Instance.Languages.FromID(classString.LanguageID);
					return htmlBuilder.Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + SummaryFileNameOnly;
					}
				else
					{
					return htmlBuilder.Database_OutputFolder(classString.Symbol.WithoutLastSegment) + '/' + SummaryFileNameOnly;
					}
				}
			}

		/* Property: SummaryFileNameOnly
		 * The file name of the topic page's summary file without the path.
		 */
		public Path SummaryFileNameOnly
			{
			get
				{
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				string nameString = classString.Symbol.LastSegment;
				return Builders.HTML.SanitizePath(nameString, true) + "-Summary.js";
				}
			}

		/* Property: SummaryToolTipsFile
		 * The path of the topic page's summary tool tips file.
		 */
		override public Path SummaryToolTipsFile
		   {  
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				if (classString.Hierarchy == ClassString.HierarchyType.Class)
					{
					var language = Engine.Instance.Languages.FromID(classString.LanguageID);
					return htmlBuilder.Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + 
								 SummaryToolTipsFileNameOnly;
					}
				else // Database
					{
					return htmlBuilder.Database_OutputFolder(classString.Symbol.WithoutLastSegment) + '/' + 
								 SummaryToolTipsFileNameOnly;
					}
				}
			}

		/* Property: SummaryToolTipsFileNameOnly
		 * The file name of the topic page's summary tool tips file without the path.
		 */
		public Path SummaryToolTipsFileNameOnly
			{
			get
				{
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				string nameString = classString.Symbol.LastSegment;
				return Builders.HTML.SanitizePath(nameString, true) + "-SummaryToolTips.js";
				}
			}



		// Group: Topic Merging Functions
		// __________________________________________________________________________


		/* Function: MergeTopics
		 * Takes a list of <Topics> that come from the same class but multiple source files and combines them into a
		 * single coherent list.  It assumes all topics from a single file will be consecutive, but otherwise the groups of
		 * topics can be in any order.
		 */
		protected void MergeTopics (List<Topic> topics)
			{
			if (topics.Count == 0)
				{  return;  }

			#if DEBUG
			// Validate that they're all from the same class and that all of a file's topics are consecutive.
			int currentFileID = topics[0].FileID;
			IDObjects.NumberSet previousFileIDs = new IDObjects.NumberSet();

			for (int i = 1; i < topics.Count; i++)
				{
				if (topics[i].ClassID != classID || topics[i].ClassString != classString)
					{  throw new Exception("All topics passed to MergeTopics() must have the object's class string and ID.");  }

				if (topics[i].FileID != currentFileID)
					{
					if (previousFileIDs.Contains(topics[i].FileID))
						{  throw new Exception("MergeTopics() requires all topics that share a file ID be consecutive.");  }

					previousFileIDs.Add(currentFileID);
					currentFileID = topics[i].FileID;
					}
				}
			#endif

			// If the first and last topic have the same file ID, that means the entire list does and we can return it as is.
			if (topics[0].FileID == topics[topics.Count-1].FileID)
				{  
				// We do still have to make sure the first topic isn't embedded though so that classes documented in lists will
				// appear correctly.
				if (topics[0].IsEmbedded)
					{
					topics[0] = topics[0].Duplicate();
					topics[0].IsEmbedded = false;
					}

				return;  
				}

			var files = Engine.Instance.Files;
			var topicTypes = Engine.Instance.TopicTypes;
			var groupTopicTypeID = topicTypes.IDFromKeyword("group");


			// First we have to sort the topic list by file name.  This ensures that the merge occurs consistently no matter
			// what order the files in the list are in or how the file IDs were assigned.

			List<Topic> sortedTopics = new List<Topic>(topics.Count);

			do
				{
				var lowestFile = files.FromID(topics[0].FileID);
				var lowestFileIndex = 0;
				var lastCheckedID = lowestFile.ID;

				for (int i = 1; i < topics.Count; i++)
					{
					if (topics[i].FileID != lastCheckedID)
						{
						var file = files.FromID(topics[i].FileID);

						if (Path.Compare(file.FileName, lowestFile.FileName) < 0)
							{  
							lowestFile = file;  
							lowestFileIndex = i;
							}

						lastCheckedID = file.ID;
						}
					}

				int count = 0;
				for (int i = lowestFileIndex; i < topics.Count && topics[i].FileID == lowestFile.ID; i++)
					{  count++;  }

				sortedTopics.AddRange( topics.GetRange(lowestFileIndex, count) );
				topics.RemoveRange(lowestFileIndex, count);
				}
			while (topics.Count > 0);


			// The topics are all in sortedTopics now, and "topics" is empty.  For clarity going forward, let's rename sortedTopics
			// to remainingTopics, since we have to move them back into topics now.

			List<Topic> remainingTopics = sortedTopics;
			sortedTopics = null;  // for safety

		
			// Find the best topic to serve as the class topic.

			int bestClassIndex = 0;
			long bestClassScore = 0;

			for (int i = 0; i < remainingTopics.Count; i++)
				{
				Topic topic = remainingTopics[i];

				if (Instance.TopicTypes.FromID(topic.TopicTypeID).Scope == TopicType.ScopeValue.Start)
					{
					long score = CodeDB.Manager.ScoreTopic(topic);

					if (score > bestClassScore)
						{
						bestClassIndex = i;
						bestClassScore = score;
						}
					}
				}


			// Copy the best topic in and everything that follows it in the file.  That will serve as the base for merging.

			int bestClassFileID = remainingTopics[bestClassIndex].FileID;
			int bestClassTopicCount = 1;

			for (int i = bestClassIndex + 1; i < remainingTopics.Count && remainingTopics[i].FileID == bestClassFileID; i++)
				{  bestClassTopicCount++;  }

			topics.AddRange( remainingTopics.GetRange(bestClassIndex, bestClassTopicCount) );
			remainingTopics.RemoveRange(bestClassIndex, bestClassTopicCount);


			// Make sure the first topic isn't embedded so that classes documented in lists still appear correctly.

			if (topics[0].IsEmbedded)
				{
				topics[0] = topics[0].Duplicate();
				topics[0].IsEmbedded = false;
				}


			// Delete all the other topics that define the class.  We don't need them anymore.

			int j = 0;
			while (j < remainingTopics.Count)
				{
				if (topicTypes.FromID(remainingTopics[j].TopicTypeID).Scope == TopicType.ScopeValue.Start)
					{  remainingTopics.RemoveAt(j);  }
				else
					{  j++;  }
				}


			// Merge any duplicate topics into the list.  This is used for things like header vs. source definitions in C++.

			// First we go through the primary topic list to handle removing list topics and merging individual topics into list
			// topics in the remaining topic list.  Everything else will be handled when iterating through the remaining topic list.

			int topicIndex = 0;
			while (topicIndex < topics.Count)
				{
				var topic = topics[topicIndex];

				// Ignore group topics
				if (topic.TopicTypeID == groupTopicTypeID)
					{  
					topicIndex++;  
					continue;
					}

				int embeddedTopicCount = CountEmbeddedTopics(topics, topicIndex);


				// We don't need to worry about enums until we do remaining topics.

				if (topic.IsEnum)
					{  topicIndex += 1 + embeddedTopicCount;  }


				// If it's not an enum and it's a standalone topic see if it will merge with an embedded topic in the remaining topic
				// list.  We don't have to worry about merging with standalone topics until we do the remaining topic list.

				else if (embeddedTopicCount == 0)
					{
					int duplicateIndex = FindDuplicateTopic(topic, remainingTopics);

					if (duplicateIndex == -1)
						{  topicIndex++;  }
					else if (remainingTopics[duplicateIndex].IsEmbedded &&
								  CodeDB.Manager.ScoreTopic(topic) < CodeDB.Manager.ScoreTopic(remainingTopics[duplicateIndex]))
						{  topics.RemoveAt(topicIndex);  }
					else
						{  topicIndex++;  }
					}


				// If it's not an enum and we're at a list topic, only remove it if EVERY member has a better definition in the other
				// list.  We can't pluck them out individually.  If even one is documented here that isn't documented elsewhere we
				// keep the entire thing in even if that leads to some duplicates.

				else
					{
					bool allHaveBetterMatches = true;

					for (int i = 0; i < embeddedTopicCount; i++)
						{
						Topic embeddedTopic = topics[topicIndex + 1 + i];
						int duplicateIndex = FindDuplicateTopic(embeddedTopic, remainingTopics);

						if (duplicateIndex == -1 ||
							 CodeDB.Manager.ScoreTopic(embeddedTopic) > CodeDB.Manager.ScoreTopic(remainingTopics[duplicateIndex]))
							{
							allHaveBetterMatches = false;
							break;
							}
						}

					if (allHaveBetterMatches)
						{  topics.RemoveRange(topicIndex, 1 + embeddedTopicCount);  }
					else
						{  topicIndex += 1 + embeddedTopicCount;  }
					}
				}


			// Now do a more comprehensive merge of the remaining topics into the primary topic list.

			int remainingTopicIndex = 0;
			while (remainingTopicIndex < remainingTopics.Count)
				{
				var remainingTopic = remainingTopics[remainingTopicIndex];

				// Ignore group topics
				if (remainingTopic.TopicTypeID == groupTopicTypeID)
					{  
					remainingTopicIndex++;  
					continue;
					}

				int embeddedTopicCount = CountEmbeddedTopics(remainingTopics, remainingTopicIndex);


				// If we're merging enums, the one with the most embedded topics (documented values) wins.  In practice one
				// should be documented and one shouldn't be, so this will be any number versus zero.

				if (remainingTopic.IsEnum)
					{
					int duplicateIndex = FindDuplicateTopic(remainingTopic, topics);

					if (duplicateIndex == -1)
						{  remainingTopicIndex += 1 + embeddedTopicCount;  }
					else
						{
						int duplicateEmbeddedTopicCount = CountEmbeddedTopics(topics, duplicateIndex);

						if (embeddedTopicCount > duplicateEmbeddedTopicCount ||
							 ( embeddedTopicCount == duplicateEmbeddedTopicCount &&
								CodeDB.Manager.ScoreTopic(remainingTopic) > CodeDB.Manager.ScoreTopic(topics[duplicateIndex]) ) )
							{
							topics.RemoveRange(duplicateIndex, 1 + duplicateEmbeddedTopicCount);
							topics.InsertRange(duplicateIndex, remainingTopics.GetRange(remainingTopicIndex, 1 + embeddedTopicCount));
							}

						remainingTopics.RemoveRange(remainingTopicIndex, 1 + embeddedTopicCount);
						}
					}


				// If it's not an enum and it's a standalone topic the one with the best score wins.

				else if (embeddedTopicCount == 0)
					{
					int duplicateIndex = FindDuplicateTopic(remainingTopic, topics);

					if (duplicateIndex == -1)
						{  remainingTopicIndex++;  }
					else if (CodeDB.Manager.ScoreTopic(remainingTopic) > CodeDB.Manager.ScoreTopic(topics[duplicateIndex]))
						{  
						if (topics[duplicateIndex].IsEmbedded)
							{  
							// Just leave them both in
							remainingTopicIndex++;  
							}
						else
							{
							topics[duplicateIndex] = remainingTopic;  
							remainingTopics.RemoveAt(remainingTopicIndex);
							}
						}
					else
						{  remainingTopics.RemoveAt(remainingTopicIndex);  }
					}


				// If it's not an enum and we're at a list topic, only remove it if EVERY member has a better definition in the other
				// list.  We can't pluck them out individually.  If even one is documented here that isn't documented elsewhere we
				// keep the entire thing in even if that leads to some duplicates.

				else
					{
					bool allHaveBetterMatches = true;

					for (int i = 0; i < embeddedTopicCount; i++)
						{
						Topic embeddedTopic = remainingTopics[remainingTopicIndex + 1 + i];
						int duplicateIndex = FindDuplicateTopic(embeddedTopic, topics);

						if (duplicateIndex == -1 ||
							 CodeDB.Manager.ScoreTopic(embeddedTopic) > CodeDB.Manager.ScoreTopic(topics[duplicateIndex]))
							{
							allHaveBetterMatches = false;
							break;
							}
						}

					if (allHaveBetterMatches)
						{  remainingTopics.RemoveRange(remainingTopicIndex, 1 + embeddedTopicCount);  }
					else
						{  remainingTopicIndex += 1 + embeddedTopicCount;  }
					}
				}


			// Generate groups from the topic lists.

			// Start at 1 to skip the class topic.
			// Don't group by file ID because topics from other files may have been combined into the list.
			var groupedTopics = GetTopicGroups(topics, 1, false);

			var groupedRemainingTopics = GetTopicGroups(remainingTopics);


			// Delete any empty groups.  We do this on the main group list too for consistency.

			j = 0;
			while (j < groupedTopics.Groups.Count)
				{
				if (groupedTopics.Groups[j].IsEmpty)
					{  groupedTopics.RemoveGroupAndTopics(j);  }
				else
					{  j++;  }
				}

			j = 0;
			while (j < groupedRemainingTopics.Groups.Count)
				{
				if (groupedRemainingTopics.Groups[j].IsEmpty)
					{  groupedRemainingTopics.RemoveGroupAndTopics(j);  }
				else
					{  j++;  }
				}


			// Now merge groups.  If any remaining groups match the title of an existing group, move its members to
			// the end of the existing group.

			int remainingGroupIndex = 0;
			while (remainingGroupIndex < groupedRemainingTopics.Groups.Count)
				{
				var remainingGroup = groupedRemainingTopics.Groups[remainingGroupIndex];
				bool merged = false;

				if (remainingGroup.Title != null)
					{
					for (int groupIndex = 0; groupIndex < groupedTopics.Groups.Count; groupIndex++)
						{
						if (groupedTopics.Groups[groupIndex].Title == remainingGroup.Title)
							{
							groupedRemainingTopics.MergeGroupInto(remainingGroupIndex, groupedTopics, groupIndex);
							merged = true;
							break;
							}
						}
					}

				if (merged == false)
					{  remainingGroupIndex++;  }
				}


			// Move any groups with titles that didn't match to the other list.  We insert it after the last group
			// of the same dominant type so function groups stay with other function groups, variable groups stay
			// with other variable groups, etc.

			remainingGroupIndex = 0;
			while (remainingGroupIndex < groupedRemainingTopics.Groups.Count)
				{
				var remainingGroup = groupedRemainingTopics.Groups[remainingGroupIndex];

				if (remainingGroup.Title != null)
					{
					int bestMatchIndex = -1;

					// Walk the list backwards because we want it to be after the last group of the type, not the first.
					for (int i = groupedTopics.Groups.Count - 1; i >= 0; i--)
						{
						if (groupedTopics.Groups[i].DominantTypeID == remainingGroup.DominantTypeID)
							{
							bestMatchIndex = i;
							break;
							}
						}

					if (bestMatchIndex == -1)
						{  
						// Just add them to the end if nothing matches.
						groupedRemainingTopics.MoveGroupTo(remainingGroupIndex, groupedTopics);  
						}
					else
						{  groupedRemainingTopics.MoveGroupTo(remainingGroupIndex, groupedTopics, bestMatchIndex + 1);  }
					}
				else
					{  remainingGroupIndex++;  }
				}


			// Now we're left with topics that are not in groups.  See if the list contains any titled groups at all.

			bool groupsWithTitles = false;

			foreach (var group in groupedTopics.Groups)
				{
				if (group.Title != null)
					{
					groupsWithTitles = true;
					break;
					}
				}


			// If there's no titles we can just append the remaining topics as is.

			if (groupsWithTitles == false)
				{
				groupedTopics.Topics.AddRange(groupedRemainingTopics.Topics);
				}


			// If there are titled groups, see if we can add them to the end of existing groups.  However, only do
			// this if TitleMatchesType is set.  It's okay to put random functions into the group "Functions" but
			// not into something more specific.  If there aren't appropriate groups to do this with, create new ones.

			else
				{
				// We don't care about the remaining groups anymore so we can just work directly on the topics.
				remainingTopics = groupedRemainingTopics.Topics;
				groupedRemainingTopics = null;  // for safety

				while (remainingTopics.Count > 0)
					{
					int type = remainingTopics[0].TopicTypeID;
					int matchingGroupIndex = -1;

					for (int i = groupedTopics.Groups.Count - 1; i >= 0; i--)
						{
						if (groupedTopics.Groups[i].DominantTypeID == type && 
							 groupedTopics.Groups[i].TitleMatchesType)
							{  
							matchingGroupIndex = i;
							break;
							}
						}

					// Create a new group if there's no existing one we can use.
					if (matchingGroupIndex == -1)
						{
						Topic generatedTopic = new Topic();
						generatedTopic.TopicID = 0;
						generatedTopic.Title = Engine.Instance.TopicTypes.FromID(type).PluralDisplayName;
						generatedTopic.Symbol = SymbolString.FromPlainText_ParenthesesAlreadyRemoved(generatedTopic.Title);
						generatedTopic.ClassString = topics[0].ClassString;
						generatedTopic.ClassID = topics[0].ClassID;
						generatedTopic.TopicTypeID = Engine.Instance.TopicTypes.IDFromKeyword("group");
						generatedTopic.FileID = topics[0].FileID;
						generatedTopic.LanguageID = topics[0].LanguageID;

						// In case there's nothing that defines the "group" keyword.
						if (generatedTopic.TopicTypeID != 0)
							{
							groupedTopics.Topics.Add(generatedTopic);
							groupedTopics.CreateGroup(groupedTopics.Topics.Count - 1, 1);
							}

						matchingGroupIndex = groupedTopics.Groups.Count - 1;
						}

					j = 0;
					while (j < remainingTopics.Count)
						{
						var remainingTopic = remainingTopics[j];
						if (remainingTopic.TopicTypeID == type)
							{
							groupedTopics.AppendToGroup(matchingGroupIndex, remainingTopic);
							remainingTopics.RemoveAt(j);
							}
						else
							{  j++;  }
						}
					}
				}
			}


		/* Function: FindDuplicateTopic
		 * Returns the index of a topic that defines the same code element as the passed one, or -1 if there isn't
		 * any.  Topics are considered duplicates if <Language.IsSameCodeElement> returns true.
		 */
		protected int FindDuplicateTopic (Topic topic, IList<Topic> listToSearch)
			{
			Language language = Engine.Instance.Languages.FromID(topic.LanguageID);

			for (int i = 0; i < listToSearch.Count; i++)
				{
				if (language.IsSameCodeElement(topic, listToSearch[i]))
					{  return i;  }
				}

			return -1;
			}


		/* Function: CountEmbeddedTopics
		 * Returns the number of embedded topics that follow the one at list index.
		 */
		protected int CountEmbeddedTopics (IList<Topic> topicList, int index)
			{
			#if DEBUG
			if (topicList[index].IsEmbedded)
				{  throw new Exception("The topic at the index passed to CountEmbeddedTopics() must not itself be embedded.");  }
			#endif

			int endOfEmbeddedTopics = index + 1;

			while (endOfEmbeddedTopics < topicList.Count && topicList[endOfEmbeddedTopics].IsEmbedded)
				{  endOfEmbeddedTopics++;  }

			return endOfEmbeddedTopics - index - 1;
			}


		/* Function: GetTopicGroups
		 * Returns a list of <TopicGroups> for the passed <Topics>.
		 */
		protected GroupedTopics GetTopicGroups (List<Topic> topics, int startingIndex = 0, bool groupByFileID = true)
			{
			GroupedTopics groupedTopics = new GroupedTopics(topics);

			int groupTopicTypeID = Engine.Instance.TopicTypes.IDFromKeyword("group");

			int i = startingIndex;
			while (i < topics.Count)
				{
				int fileID = topics[i].FileID;
				int groupStart = i;
				int groupCount = 1;

				i++;

				while (i < topics.Count && 
							(topics[i].FileID == fileID || !groupByFileID) && 
							topics[i].TopicTypeID != groupTopicTypeID)
					{
					groupCount++;
					i++;
					}

				groupedTopics.CreateGroup(groupStart, groupCount);
				}

			return groupedTopics;
			}




		// Group: Variables
		// __________________________________________________________________________


		/* var: classID
		 * The ID of the class that this object is building.
		 */
		protected int classID;

		/* var: classString
		 * The <Symbols.ClassString> associated with <classID>.
		 */
		protected Symbols.ClassString classString;

		}
	}

