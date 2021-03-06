#include <string.h>
#include <winsock2.h>

// credits to aluigi for following 3 functions

unsigned int resolve(char *host) 
{
    struct    hostent    *hp;
    unsigned int    host_ip;

    host_ip = inet_addr(host);
    if(host_ip == INADDR_NONE) 
	{
        hp = gethostbyname(host);
        if (hp == 0) 
		{
            return 0;
        } 
		else host_ip = *(u_int *)(hp->h_addr);
    }

    return(host_ip);
}


static unsigned char gsvalfunc(int reg) 
{
    if(reg < 26) 
		return(reg + 'A');
    if(reg < 52) 
		return(reg + 'G');
    if(reg < 62)
		return(reg - 4);
    if(reg == 62) 
		return('+');
    if(reg == 63) 
		return('/');
    return(0);
}


unsigned char *gsseckey(unsigned char *dst, unsigned char *src)
{
    int             i,
                    x,
                    y,
                    num,
                    num2,
                    size,
                    keysz;
    unsigned char   enctmp[256],
                    *p;

	const static unsigned char key[] = "Av3M99";

    const static unsigned char enctype1_data[256] = /* pre-built */
        "\x01\xba\xfa\xb2\x51\x00\x54\x80\x75\x16\x8e\x8e\x02\x08\x36\xa5"
        "\x2d\x05\x0d\x16\x52\x07\xb4\x22\x8c\xe9\x09\xd6\xb9\x26\x00\x04"
        "\x06\x05\x00\x13\x18\xc4\x1e\x5b\x1d\x76\x74\xfc\x50\x51\x06\x16"
        "\x00\x51\x28\x00\x04\x0a\x29\x78\x51\x00\x01\x11\x52\x16\x06\x4a"
        "\x20\x84\x01\xa2\x1e\x16\x47\x16\x32\x51\x9a\xc4\x03\x2a\x73\xe1"
        "\x2d\x4f\x18\x4b\x93\x4c\x0f\x39\x0a\x00\x04\xc0\x12\x0c\x9a\x5e"
        "\x02\xb3\x18\xb8\x07\x0c\xcd\x21\x05\xc0\xa9\x41\x43\x04\x3c\x52"
        "\x75\xec\x98\x80\x1d\x08\x02\x1d\x58\x84\x01\x4e\x3b\x6a\x53\x7a"
        "\x55\x56\x57\x1e\x7f\xec\xb8\xad\x00\x70\x1f\x82\xd8\xfc\x97\x8b"
        "\xf0\x83\xfe\x0e\x76\x03\xbe\x39\x29\x77\x30\xe0\x2b\xff\xb7\x9e"
        "\x01\x04\xf8\x01\x0e\xe8\x53\xff\x94\x0c\xb2\x45\x9e\x0a\xc7\x06"
        "\x18\x01\x64\xb0\x03\x98\x01\xeb\x02\xb0\x01\xb4\x12\x49\x07\x1f"
        "\x5f\x5e\x5d\xa0\x4f\x5b\xa0\x5a\x59\x58\xcf\x52\x54\xd0\xb8\x34"
        "\x02\xfc\x0e\x42\x29\xb8\xda\x00\xba\xb1\xf0\x12\xfd\x23\xae\xb6"
        "\x45\xa9\xbb\x06\xb8\x88\x14\x24\xa9\x00\x14\xcb\x24\x12\xae\xcc"
        "\x57\x56\xee\xfd\x08\x30\xd9\xfd\x8b\x3e\x0a\x84\x46\xfa\x77\xb8";


        /* 1) buffer creation with incremental data */

    p = enctmp;
    for(i = 0; i < 256; i++) 
	{
        *p++ = i;
    }

        /* 2) buffer scrambled with key */

    keysz = (int)strlen(key);
    p = enctmp;
    for(i = num = 0; i < 256; i++)
	{
        num = (num + *p + key[i % keysz]) & 0xff;
        x = enctmp[num];
        enctmp[num] = *p;
        *p++ = x;
    }

        /* 3) source string scrambled with the buffer */

    p = src;
    num = num2 = 0;
    while(*p) 
	{
        num = (num + *p + 1) & 0xff;
        x = enctmp[num];
        num2 = (num2 + x) & 0xff;
        y = enctmp[num2];
        enctmp[num2] = x;
        enctmp[num] = y;
        *p++ ^= enctmp[(x + y) & 0xff];
    }
    size = p - src;

        /* 4) splitting of the source string from 3 to 4 bytes */

    p = dst;
    size /= 3;
    while(size--) 
	{
        x = *src++;
        y = *src++;
        *p++ = gsvalfunc(x >> 2);
        *p++ = gsvalfunc(((x & 3) << 4) | (y >> 4));
        x = *src++;
        *p++ = gsvalfunc(((y & 15) << 2) | (x >> 6));
        *p++ = gsvalfunc(x & 63);
    }
    *p = 0;

    return(dst);
}
