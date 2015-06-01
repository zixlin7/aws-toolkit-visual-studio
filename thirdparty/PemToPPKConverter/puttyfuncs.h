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
#ifndef FALSE
#define FALSE               0
#endif

#ifndef TRUE
#define TRUE                1
#endif

typedef unsigned int word32;
typedef unsigned int uint32;

#define INT_MAX       2147483647    /* maximum (signed) int value */
#define rol(x,y) ( ((x) << (y)) | (((uint32)x) >> (32-y)) )

#define smalloc(z) safemalloc(z,1)
#define snmalloc safemalloc
#define srealloc(y,z) saferealloc(y,z,1)
#define snrealloc saferealloc
#define sfree safefree

#define snew(type) ((type *)snmalloc(1, sizeof(type)))
#define snewn(n, type) ((type *)snmalloc((n), sizeof(type)))
#define sresize(ptr, n, type) ((type *)snrealloc((ptr), (n), sizeof(type)))

#define PUT_32BIT_MSB_FIRST(cp, value) ( \
  (cp)[0] = (unsigned char)((value) >> 24), \
  (cp)[1] = (unsigned char)((value) >> 16), \
  (cp)[2] = (unsigned char)((value) >> 8), \
  (cp)[3] = (unsigned char)(value) )

#define PUT_32BIT(cp, value) PUT_32BIT_MSB_FIRST(cp, value)

#define GET_32BIT_MSB_FIRST(cp) \
  (((unsigned long)(unsigned char)(cp)[0] << 24) | \
  ((unsigned long)(unsigned char)(cp)[1] << 16) | \
  ((unsigned long)(unsigned char)(cp)[2] << 8) | \
  ((unsigned long)(unsigned char)(cp)[3]))

#define GET_32BIT(cp) GET_32BIT_MSB_FIRST(cp)


#define isbase64(c) (    ((c) >= 'A' && (c) <= 'Z') || \
                         ((c) >= 'a' && (c) <= 'z') || \
                         ((c) >= '0' && (c) <= '9') || \
                         (c) == '+' || (c) == '/' || (c) == '=' \
                         )


typedef struct Filename {
    char path[260];
} Filename;

#define f_open(filename, mode, isprivate) ( fopen((filename).path, (mode)) )


struct ssh_signkey {
    void *(*newkey) (char *data, int len);
    void (*freekey) (void *key);
    char *(*fmtkey) (void *key);
    unsigned char *(*public_blob) (void *key, int *len);
    unsigned char *(*private_blob) (void *key, int *len);
    void *(*createkey) (unsigned char *pub_blob, int pub_len,
			unsigned char *priv_blob, int priv_len);
    void *(*openssh_createkey) (unsigned char **blob, int *len);
    int (*openssh_fmtkey) (void *key, unsigned char *blob, int len);
    int (*pubkey_bits) (void *blob, int len);
    char *(*fingerprint) (void *key);
    int (*verifysig) (void *key, char *sig, int siglen,
		      char *data, int datalen);
    unsigned char *(*sign) (void *key, char *data, int datalen,
			    int *siglen);
    char *name;
    char *keytype;		       /* for host key cache */
};

struct ssh2_userkey {
    const struct ssh_signkey *alg;     /* the key algorithm */
    void *data;			       /* the key data */
    char *comment;		       /* the key comment */
};

typedef struct {
    uint32 h[5];
    unsigned char block[64];
    int blkused;
    uint32 lenhi, lenlo;
} SHA_State;

struct openssh_key {
    int type;
    int encrypted;
    char iv[32];
    unsigned char *keyblob;
    int keyblob_len, keyblob_size;
};

void *safemalloc(size_t n, size_t size);

void SHA_Core_Init(uint32 h[5]);
void SHA_Init(SHA_State * s);
void SHATransform(word32 * digest, word32 * block);
void SHA_Bytes(SHA_State * s, void *p, int len);
void SHA_Final(SHA_State * s, unsigned char *output);
void SHA_Simple(void *p, int len, unsigned char *output);
void sha1_key_internal(void *handle, unsigned char *key, int len);

void hmac_sha1_simple(void *key, int keylen, void *data, int datalen, unsigned char *output);

int base64_lines(int datalen);
void base64_encode_atom(unsigned char *data, int n, char *out);
void base64_encode(FILE * fp, unsigned char *data, int datalen, int cpl);
int base64_decode_atom(char *atom, unsigned char *out);

void ssh2_save_userkey(const struct Filename *filename, struct ssh2_userkey *key, char *passphrase);


void saveKey(const struct Filename *filename, struct ssh2_userkey *key, char *passphrase);

char *fgetline(FILE *fp);
void strip_crlf(char *str);
char *dupstr(const char *s);