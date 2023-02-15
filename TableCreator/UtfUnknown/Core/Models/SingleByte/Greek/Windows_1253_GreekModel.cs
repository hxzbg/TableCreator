/* ***** BEGIN LICENSE BLOCK *****
 * Version: MPL 1.1/GPL 2.0/LGPL 2.1
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is Mozilla Universal charset detector code.
 *
 * The Initial Developer of the Original Code is
 * Netscape Communications Corporation.
 * Portions created by the Initial Developer are Copyright (C) 2001
 * the Initial Developer. All Rights Reserved.
 *
 * Contributor(s):
 *          Shy Shalom <shooshX@gmail.com>
 *          Rudi Pettazzi <rudi.pettazzi@gmail.com> (C# port)
 * 
 * Alternatively, the contents of this file may be used under the terms of
 * either the GNU General Public License Version 2 or later (the "GPL"), or
 * the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
 * in which case the provisions of the GPL or the LGPL are applicable instead
 * of those above. If you wish to allow use of your version of this file only
 * under the terms of either the GPL or the LGPL, and not to allow others to
 * use your version of this file under the terms of the MPL, indicate your
 * decision by deleting the provisions above and replace them with the notice
 * and other provisions required by the GPL or the LGPL. If you do not delete
 * the provisions above, a recipient may use your version of this file under
 * the terms of any one of the MPL, the GPL or the LGPL.
 *
 * ***** END LICENSE BLOCK ***** */

/*
* The following part was imported from https://gitlab.freedesktop.org/uchardet/uchardet
* The implementation of this feature was originally done on https://gitlab.freedesktop.org/uchardet/uchardet/blob/master/src/LangModels/LangGreekModel.cpp
* and adjusted to language specific support.
*/

namespace UtfUnknown.Core.Models.SingleByte.Greek
{
    public class Windows_1253_GreekModel : GreekModel
    {
        // Generated by BuildLangModel.py
        // On: 2016-05-25 15:21:50.073117

        // Character Mapping Table:
        // ILL: illegal character.
        // CTR: control character specific to the charset.
        // RET: carriage/return.
        // SYM: symbol (punctuation) that does not belong to word.
        // NUM: 0 - 9.
        // 
        // Other characters are ordered by probabilities
        // (0 is the most common character in the language).
        // 
        // Orders are generic to a language.So the codepoint with order X in
        // CHARSET1 maps to the same character as the codepoint with the same
        // order X in CHARSET2 for the same language.
        // As such, it is possible to get missing order.For instance the
        // ligature of 'o' and 'e' exists in ISO-8859-15 but not in ISO-8859-1
        // even though they are both used for French.Same for the euro sign.

        //Character Mapping Table:
        private readonly static byte[] CHAR_TO_ORDER_MAP = {
          CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR,RET,CTR,CTR,RET,CTR,CTR, /* 0X */
          CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR,CTR, /* 1X */
          SYM,SYM,SYM,SYM,SYM,SYM,SYM,SYM,SYM,SYM,SYM,SYM,SYM,SYM,SYM,SYM, /* 2X */
          NUM,NUM,NUM,NUM,NUM,NUM,NUM,NUM,NUM,NUM,SYM,SYM,SYM,SYM,SYM,SYM, /* 3X */
          SYM, 32, 46, 41, 40, 30, 52, 48, 42, 33, 56, 49, 39, 44, 36, 34, /* 4X */
           47, 59, 35, 38, 37, 43, 54, 50, 58, 53, 57,SYM,SYM,SYM,SYM,SYM, /* 5X */
          SYM, 32, 46, 41, 40, 30, 52, 48, 42, 33, 56, 49, 39, 44, 36, 34, /* 6X */
           47, 59, 35, 38, 37, 43, 54, 50, 58, 53, 57,SYM,SYM,SYM,SYM,CTR, /* 7X */
          SYM,ILL,SYM,SYM,SYM,SYM,SYM,SYM,ILL,SYM,ILL,SYM,ILL,ILL,ILL,ILL, /* 8X */
          ILL,SYM,SYM,SYM,SYM,SYM,SYM,SYM,ILL,SYM,ILL,SYM,ILL,ILL,ILL,ILL, /* 9X */
          SYM,SYM, 17,SYM,SYM,SYM,SYM,SYM,SYM,SYM,ILL,SYM,SYM,SYM,SYM,SYM, /* AX */
          SYM,SYM,SYM,SYM,SYM, 62,SYM,SYM, 19, 22, 15,SYM, 16,SYM, 24, 28, /* BX */
           55,  0, 25, 18, 20,  5, 29, 10, 26,  3,  8, 14, 13,  4, 31,  1, /* CX */
           11,  6,ILL,  7,  2, 12, 27, 23, 45, 21, 51, 60, 17, 19, 22, 15, /* DX */
           61,  0, 25, 18, 20,  5, 29, 10, 26,  3,  8, 14, 13,  4, 31,  1, /* EX */
           11,  6,  9,  7,  2, 12, 27, 23, 45, 21, 51, 60, 16, 24, 28,ILL, /* FX */
        };
        /*X0  X1  X2  X3  X4  X5  X6  X7  X8  X9  XA  XB  XC  XD  XE  XF */

        public Windows_1253_GreekModel() : base(CHAR_TO_ORDER_MAP, CodepageName.WINDOWS_1253)
        {
        }
    }
}