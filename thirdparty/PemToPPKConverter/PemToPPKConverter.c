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

// PemToPPKConverter.cpp : Defines the entry point for the application.
//

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files:
#include <windows.h>

// C RunTime Header Files
#include <stdlib.h>
#include <malloc.h>
#include <memory.h>
#include <tchar.h>
#include <Shellapi.h>
#include <stdio.h>

#include "main_putty_funcs.h"


int APIENTRY _tWinMain(HINSTANCE hInstance,
                     HINSTANCE hPrevInstance,
                     LPTSTR    lpCmdLine,
                     int       nCmdShow)
{
	const char *errorMessage;
	const char *passphrase = NULL;
	struct ssh2_userkey* key;

   int    argc;
    char** argv;
	char* firstArg;
	char* secondArg;

    int    index;
    int    result;

    // count the arguments
    
    char* arg  = (char*)lpCmdLine;
    

	int startPos = 1;
	int endPos = 2;
	while(arg[endPos] != '"')
	{
		endPos++;
	}

	firstArg = malloc(endPos - startPos);
	strncpy(firstArg, arg + 1, endPos - startPos + 1);
	firstArg[endPos - startPos] = '\0';

	startPos = endPos + 2;
	endPos = startPos + 1;
	while(arg[endPos] != '"')
	{
		endPos++;
	}

	secondArg = malloc(endPos - startPos);
	strncpy(secondArg, arg + strlen(firstArg) + 4, endPos - startPos - 1);
	secondArg[endPos - startPos - 1] = '\0';

	printf("Converting from %s to %s\n", firstArg, secondArg);

	key = openssh_read(firstArg, passphrase, &errorMessage);
	saveKey(secondArg, key, passphrase);

	free(firstArg);
	free(secondArg);

	return 200;
}

