﻿/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Ruby
 * ____________________________________________________________________________
 * 
 * Additional language support for Ruby.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class Ruby : Language
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Ruby
		 */
		public Ruby (Languages.Manager manager) : base (manager, "Ruby")
			{
			}


		/* Function: ParseClassPrototype
		 * Converts a raw text prototype into a <ParsedClassPrototype>.  Will return null if it is not an appropriate prototype.
		 */
		override public ParsedClassPrototype ParseClassPrototype (string stringPrototype, int commentTypeID)
			{
			if (EngineInstance.CommentTypes.FromID(commentTypeID).Flags.ClassHierarchy == false)
				{  return null;  }

			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator startOfPrototype = tokenizedPrototype.FirstToken;
			ParsedClassPrototype parsedPrototype = new ParsedClassPrototype(tokenizedPrototype);
			bool success = false;

			success = TryToSkipClassDeclarationLine(ref startOfPrototype, ParseMode.ParseClassPrototype);

			if (success)
				{  return parsedPrototype;  }
			else
			    {  return base.ParseClassPrototype(stringPrototype, commentTypeID);  }
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipClassDeclarationLine
		 * 
		 * If the iterator is on a class's declaration line, moves it past it and returns true.  It does not handle the class body.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipClassDeclarationLine (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// Keyword

			if (lookahead.MatchesToken("class") == false)
				{  return false;  }

			if (mode == ParseMode.ParseClassPrototype)
				{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.Keyword;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			TokenIterator startOfIdentifier = lookahead;

			if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
				{  
				ResetTokensBetween(iterator, lookahead, mode);
				return false;  
				}

			if (mode == ParseMode.ParseClassPrototype)
				{  iterator.Tokenizer.SetClassPrototypeParsingTypeBetween(startOfIdentifier, lookahead, ClassPrototypeParsingType.Name);  }

			TryToSkipWhitespace(ref lookahead);


			// Base class

			if (lookahead.Character == '<')
				{
				if (mode == ParseMode.ParseClassPrototype)
					{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfParents;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				TokenIterator startOfParent = lookahead;

				if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				if (mode == ParseMode.ParseClassPrototype)
					{  lookahead.Tokenizer.SetClassPrototypeParsingTypeBetween(startOfParent, lookahead, ClassPrototypeParsingType.Name);  }
				}


			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipUnqualifiedIdentifier
		 * Tries to move the iterator past a single unqualified identifier, which means only "X" in "X.Y.Z".
		 */
		protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator)
			{
			if (iterator.FundamentalType == FundamentalType.Text)
				{
				if (iterator.Character >= '0' && iterator.Character <= '9')
					{  return false;  }
				}
			else if (iterator.FundamentalType == FundamentalType.Symbol)
				{
				if (iterator.Character != '_')
					{  return false;  }
				}
			else
				{  return false;  }

			do
				{  iterator.Next();  }
			while (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_');

			return true;
			}

		}
	}