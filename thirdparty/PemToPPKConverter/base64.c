/*
 * PuTTY is copyright 1997-2007 Simon Tatham.

 * Portions copyright Robert de Bath, Joris van Rantwijk, Delian
 * Delchev, Andreas Schultz, Jeroen Massar, Wez Furlong, Nicolas Barry,
 * Justin Bradford, Ben Harris, Malcolm Smith, Ahmad Khalifa, Markus
 * Kuhn, and CORE SDI S.A.

 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation files
 * (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software,
 * and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:

 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT.  IN NO EVENT SHALL THE COPYRIGHT HOLDERS BE LIABLE
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
 * CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#include <stdio.h>
#include <string.h>

#include "puttyfuncs.h"

int base64_lines(int datalen)
{
    /* When encoding, we use 64 chars/line, which equals 48 real chars. */
    return (datalen + 47) / 48;
}

void base64_encode_atom(unsigned char *data, int n, char *out)
{
    static const char base64_chars[] =
	"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    unsigned word;

    word = data[0] << 16;
    if (n > 1)
	word |= data[1] << 8;
    if (n > 2)
	word |= data[2];
    out[0] = base64_chars[(word >> 18) & 0x3F];
    out[1] = base64_chars[(word >> 12) & 0x3F];
    if (n > 1)
	out[2] = base64_chars[(word >> 6) & 0x3F];
    else
	out[2] = '=';
    if (n > 2)
	out[3] = base64_chars[word & 0x3F];
    else
	out[3] = '=';
}

void base64_encode(FILE * fp, unsigned char *data, int datalen, int cpl)
{
    int linelen = 0;
    char out[4];
    int n, i;

    while (datalen > 0) {
	n = (datalen < 3 ? datalen : 3);
	base64_encode_atom(data, n, out);
	data += n;
	datalen -= n;
	for (i = 0; i < 4; i++) {
	    if (linelen >= cpl) {
		linelen = 0;
		fputc('\n', fp);
	    }
	    fputc(out[i], fp);
	    linelen++;
	}
    }
    fputc('\n', fp);
}

int base64_decode_atom(char *atom, unsigned char *out)
{
    int vals[4];
    int i, v, len;
    unsigned word;
    char c;

    for (i = 0; i < 4; i++) 
	{
		c = atom[i];
		if (c >= 'A' && c <= 'Z')
			v = c - 'A';
		else if (c >= 'a' && c <= 'z')
			v = c - 'a' + 26;
		else if (c >= '0' && c <= '9')
			v = c - '0' + 52;
		else if (c == '+')
			v = 62;
		else if (c == '/')
			v = 63;
		else if (c == '=')
			v = -1;
		else
			return 0;		       /* invalid atom */
		vals[i] = v;
    }

    if (vals[0] == -1 || vals[1] == -1)
		return 0;
    if (vals[2] == -1 && vals[3] != -1)
		return 0;

    if (vals[3] != -1)
		len = 3;
    else if (vals[2] != -1)
		len = 2;
    else
		len = 1;

    word = ((vals[0] << 18) |
	    (vals[1] << 12) | ((vals[2] & 0x3F) << 6) | (vals[3] & 0x3F));
    out[0] = (word >> 16) & 0xFF;
    
	if (len > 1)
		out[1] = (word >> 8) & 0xFF;
    if (len > 2)
		out[2] = word & 0xFF;

    return len;
}