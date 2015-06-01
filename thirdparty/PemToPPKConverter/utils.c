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

void *safemalloc(size_t n, size_t size)
{
    void *p;

    if (n > INT_MAX / size) {
		p = NULL;
    } 
	else 
	{
		size *= n;
		if (size == 0) size = 1;
			p = malloc(size);
    }

    if (!p) 
	{
		char str[200];
		strcpy(str, "Out of memory!");
	}

	return p;
}

void *saferealloc(void *ptr, size_t n, size_t size)
{
    void *p;

    if (n > INT_MAX / size) 
	{
		p = NULL;
    } 
	else 
	{
		size *= n;
		if (!ptr) 
		{
			p = malloc(size);
		} 
		else 
		{
			p = realloc(ptr, size);
		}
    }

    if (!p) 
	{
		char str[200];
		strcpy(str, "Out of memory!");
		//modalfatalbox(str);
    }

	return p;
}


char *fgetline(FILE *fp)
{
    char *ret = snewn(512, char);
    int size = 512, len = 0;
    while (fgets(ret + len, size - len, fp)) 
	{
		len += strlen(ret + len);
		if (ret[len-1] == '\n')
			break;		       /* got a newline, we're done */
		size = len + 512;
		ret = sresize(ret, size, char);
    }
    if (len == 0) 
	{		       /* first fgets returned NULL */
		free(ret);
		return NULL;
    }
    ret[len] = '\0';
    return ret;
}

void strip_crlf(char *str)
{
    char *p = str + strlen(str);

    while (p > str && (p[-1] == '\r' || p[-1] == '\n'))
	*--p = '\0';
}

char *dupstr(const char *s)
{
    char *p = NULL;
    if (s) {
        int len = strlen(s);
        p = snewn(len + 1, char);
        strcpy(p, s);
    }
    return p;
}