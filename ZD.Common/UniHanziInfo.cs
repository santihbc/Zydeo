﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZD.Common
{
    /// <summary>
    /// <para>Information about a Hanzi from the Unihan database.</para>
    /// <para>Used in CHDICT's interactive "New entry" editor.</para>
    /// </summary>
    public class UniHanziInfo : IBinSerializable
    {
        /// <summary>
        /// True if character can be used in Simplified Chinese.
        /// </summary>
        public readonly bool CanBeSimp;

        /// <summary>
        /// <para>If character can be used as simplified, a list of its traditional variants.</para>
        /// <para>List may contain character itself, e.g., if traditional and simplified forms are identical.</para>
        /// <para>If character cannot be used as simplified, list is empty.</para>
        /// </summary>
        public readonly char[] TradVariants;

        /// <summary>
        /// Character's pinyin readings; more frequent first. In display form: e.g., "hǔ", not "hu3".
        /// </summary>
        public readonly string[] Pinyin;

        /// <summary>
        /// Ctor: init from data.
        /// </summary>
        public UniHanziInfo(bool canBeSimp, char[] tradVariants, string[] pinyin)
        {
            CanBeSimp = canBeSimp;
            if (tradVariants.Length > 127) throw new ArgumentException("Maximum allowed number of traditional variants is 127.");
            if (tradVariants.Length == 0) throw new ArgumentException("At least 1 traditional variant required; can be character itself.");
            TradVariants = new char[tradVariants.Length];
            for (int i = 0; i != TradVariants.Length; ++i) TradVariants[i] = tradVariants[i];
            if (pinyin.Length > 127) throw new ArgumentException("Maximum allowed number of Pinyin readings is 127.");
            if (pinyin.Length == 0) throw new ArgumentException("At least one Pinyin reading required.");
            Pinyin = new string[pinyin.Length];
            for (int i = 0; i != Pinyin.Length; ++i) Pinyin[i] = pinyin[i];
        }

        /// <summary>
        /// Ctor: serialize from binary.
        /// </summary>
        public UniHanziInfo(BinReader br)
        {
            byte b = br.ReadByte();
            if (b == 0) CanBeSimp = false;
            else CanBeSimp = true;
            b = br.ReadByte();
            TradVariants = new char[b];
            for (byte i = 0; i != b; ++i) TradVariants[i] = br.ReadChar();
            b = br.ReadByte();
            Pinyin = new string[b];
            for (byte i = 0; i != b; ++i) Pinyin[i] = br.ReadString();
        }

        /// <summary>
        /// Serialize to binary.
        /// </summary>
        public void Serialize(BinWriter bw)
        {
            if (CanBeSimp) bw.WriteByte(1);
            else bw.WriteByte(0);
            bw.WriteByte((byte)TradVariants.Length);
            foreach (char c in TradVariants) bw.WriteChar(c);
            bw.WriteByte((byte)Pinyin.Length);
            foreach (string str in Pinyin) bw.WriteString(str);
        }
    }
}