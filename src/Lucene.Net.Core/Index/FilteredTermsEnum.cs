using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Abstract class for enumerating a subset of all terms.
    ///
    /// <p>Term enumerations are always ordered by
    /// <seealso cref="#getComparator"/>.  Each term in the enumeration is
    /// greater than all that precede it.</p>
    /// <p><em>Please note:</em> Consumers of this enum cannot
    /// call {@code seek()}, it is forward only; it throws
    /// <seealso cref="UnsupportedOperationException"/> when a seeking method
    /// is called.
    /// </summary>
    public abstract class FilteredTermsEnum : TermsEnum
    {
        private BytesRef initialSeekTerm = null;
        private bool doSeek;
        private BytesRef actualTerm = null;

        private readonly TermsEnum tenum;

        /// <summary>
        /// Return value, if term should be accepted or the iteration should
        /// {@code END}. The {@code *_SEEK} values denote, that after handling the current term
        /// the enum should call <seealso cref="#nextSeekTerm"/> and step forward. </summary>
        /// <seealso cref= #accept(BytesRef) </seealso>
        protected internal enum AcceptStatus
        {
            /// <summary>
            /// Accept the term and position the enum at the next term. </summary>
            YES,

            /// <summary>
            /// Accept the term and advance (<seealso cref="FilteredTermsEnum#nextSeekTerm(BytesRef)"/>)
            /// to the next term.
            /// </summary>
            YES_AND_SEEK,

            /// <summary>
            /// Reject the term and position the enum at the next term. </summary>
            NO,

            /// <summary>
            /// Reject the term and advance (<seealso cref="FilteredTermsEnum#nextSeekTerm(BytesRef)"/>)
            /// to the next term.
            /// </summary>
            NO_AND_SEEK,

            /// <summary>
            /// Reject the term and stop enumerating. </summary>
            END
        }

        /// <summary>
        /// Return if term is accepted, not accepted or the iteration should ended
        /// (and possibly seek).
        /// </summary>
        protected abstract AcceptStatus Accept(BytesRef term);

        /// <summary>
        /// Creates a filtered <seealso cref="TermsEnum"/> on a terms enum. </summary>
        /// <param name="tenum"> the terms enumeration to filter. </param>
        public FilteredTermsEnum(TermsEnum tenum)
            : this(tenum, true)
        {
        }

        /// <summary>
        /// Creates a filtered <seealso cref="TermsEnum"/> on a terms enum. </summary>
        /// <param name="tenum"> the terms enumeration to filter. </param>
        public FilteredTermsEnum(TermsEnum tenum, bool startWithSeek)
        {
            Debug.Assert(tenum != null);
            this.tenum = tenum;
            doSeek = startWithSeek;
        }

        /// <summary>
        /// Use this method to set the initial <seealso cref="BytesRef"/>
        /// to seek before iterating. this is a convenience method for
        /// subclasses that do not override <seealso cref="#nextSeekTerm"/>.
        /// If the initial seek term is {@code null} (default),
        /// the enum is empty.
        /// <P>You can only use this method, if you keep the default
        /// implementation of <seealso cref="#nextSeekTerm"/>.
        /// </summary>
        protected void SetInitialSeekTerm(BytesRef term)
        {
            this.initialSeekTerm = term;
        }

        /// <summary>
        /// On the first call to <seealso cref="#next"/> or if <seealso cref="#accept"/> returns
        /// <seealso cref="AcceptStatus#YES_AND_SEEK"/> or <seealso cref="AcceptStatus#NO_AND_SEEK"/>,
        /// this method will be called to eventually seek the underlying TermsEnum
        /// to a new position.
        /// On the first call, {@code currentTerm} will be {@code null}, later
        /// calls will provide the term the underlying enum is positioned at.
        /// this method returns per default only one time the initial seek term
        /// and then {@code null}, so no repositioning is ever done.
        /// <p>Override this method, if you want a more sophisticated TermsEnum,
        /// that repositions the iterator during enumeration.
        /// If this method always returns {@code null} the enum is empty.
        /// <p><em>Please note:</em> this method should always provide a greater term
        /// than the last enumerated term, else the behaviour of this enum
        /// violates the contract for TermsEnums.
        /// </summary>
        protected virtual BytesRef NextSeekTerm(BytesRef currentTerm)
        {
            BytesRef t = initialSeekTerm;
            initialSeekTerm = null;
            return t;
        }

        /// <summary>
        /// Returns the related attributes, the returned <seealso cref="AttributeSource"/>
        /// is shared with the delegate {@code TermsEnum}.
        /// </summary>
        public override AttributeSource Attributes
        {
            get { return tenum.Attributes; }
        }

        public override BytesRef Term
        {
            get { return tenum.Term; }
        }

        public override IComparer<BytesRef> Comparator
        {
            get
            {
                return tenum.Comparator;
            }
        }

        public override int DocFreq()
        {
            return tenum.DocFreq();
        }

        public override long TotalTermFreq()
        {
            return tenum.TotalTermFreq();
        }

        /// <summary>
        /// this enum does not support seeking! </summary>
        /// <exception cref="UnsupportedOperationException"> In general, subclasses do not
        ///         support seeking. </exception>
        public override bool SeekExact(BytesRef term)
        {
            throw new System.NotSupportedException(this.GetType().Name + " does not support seeking");
        }

        /// <summary>
        /// this enum does not support seeking! </summary>
        /// <exception cref="UnsupportedOperationException"> In general, subclasses do not
        ///         support seeking. </exception>
        public override SeekStatus SeekCeil(BytesRef term)
        {
            throw new System.NotSupportedException(this.GetType().Name + " does not support seeking");
        }

        /// <summary>
        /// this enum does not support seeking! </summary>
        /// <exception cref="UnsupportedOperationException"> In general, subclasses do not
        ///         support seeking. </exception>
        public override void SeekExact(long ord)
        {
            throw new System.NotSupportedException(this.GetType().Name + " does not support seeking");
        }

        public override long Ord()
        {
            return tenum.Ord();
        }

        public override DocsEnum Docs(Bits bits, DocsEnum reuse, int flags)
        {
            return tenum.Docs(bits, reuse, flags);
        }

        public override DocsAndPositionsEnum DocsAndPositions(Bits bits, DocsAndPositionsEnum reuse, int flags)
        {
            return tenum.DocsAndPositions(bits, reuse, flags);
        }

        /// <summary>
        /// this enum does not support seeking! </summary>
        /// <exception cref="UnsupportedOperationException"> In general, subclasses do not
        ///         support seeking. </exception>
        public override void SeekExact(BytesRef term, TermState state)
        {
            throw new System.NotSupportedException(this.GetType().Name + " does not support seeking");
        }

        /// <summary>
        /// Returns the filtered enums term state
        /// </summary>
        public override TermState TermState()
        {
            Debug.Assert(tenum != null);
            return tenum.TermState();
        }

        public override BytesRef Next()
        {
            //System.out.println("FTE.next doSeek=" + doSeek);
            //new Throwable().printStackTrace(System.out);
            for (; ; )
            {
                // Seek or forward the iterator
                if (doSeek)
                {
                    doSeek = false;
                    BytesRef t = NextSeekTerm(actualTerm);
                    //System.out.println("  seek to t=" + (t == null ? "null" : t.utf8ToString()) + " tenum=" + tenum);
                    // Make sure we always seek forward:
                    Debug.Assert(actualTerm == null || t == null || Comparator.Compare(t, actualTerm) > 0, "curTerm=" + actualTerm + " seekTerm=" + t);
                    if (t == null || tenum.SeekCeil(t) == SeekStatus.END)
                    {
                        // no more terms to seek to or enum exhausted
                        //System.out.println("  return null");
                        return null;
                    }
                    actualTerm = tenum.Term;
                    //System.out.println("  got term=" + actualTerm.utf8ToString());
                }
                else
                {
                    actualTerm = tenum.Next();
                    if (actualTerm == null)
                    {
                        // enum exhausted
                        return null;
                    }
                }

                // check if term is accepted
                switch (Accept(actualTerm))
                {
                    case FilteredTermsEnum.AcceptStatus.YES_AND_SEEK:
                        doSeek = true;
                        // term accepted, but we need to seek so fall-through
                        goto case FilteredTermsEnum.AcceptStatus.YES;
                    case FilteredTermsEnum.AcceptStatus.YES:
                        // term accepted
                        return actualTerm;

                    case FilteredTermsEnum.AcceptStatus.NO_AND_SEEK:
                        // invalid term, seek next time
                        doSeek = true;
                        break;

                    case FilteredTermsEnum.AcceptStatus.END:
                        // we are supposed to end the enum
                        return null;
                }
            }
        }
    }
}