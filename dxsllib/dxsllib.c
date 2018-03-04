#include <windows.h>
#include <stdio.h>
#include "dxsllib.h"
#include "gs.h"

#define MAX_RECV_BUFF		1024
#define STR_SECURE			"\\secure\\"
#define VLD_PART1			"\\gamename\\deusex\\location\\0\\validate\\"
#define VLD_PART2			"\\final\\\\list\\\\gamename\\deusex\\final\\"
#define STR_FINAL			"\\final\\"
#define STR_IP				"\\ip\\"
#define STR_STATUS			"\\status\\"
#define STR_INFO			"\\info\\"

#define STR_OPTION_HOSTNAME			"hostname"
#define STR_OPTION_MAPNAME			"mapname"
#define STR_OPTION_HOSTPORT			"hostport"
#define STR_OPTION_GAMETYPE			"gametype"


typedef struct _list_players_s
{
	int						id;
	char					nick[32];
	int						frags;
	int						ping;
	int						team;
	struct _list_players_s	*next;
} list_players_s;


typedef struct _list_gameservers_s
{
	char						ip[16];
	unsigned long				net_ip;
	unsigned short				port;
	struct _list_gameservers_s	*next;
	list_players_s				*players;
	struct server_data_s		server_data;
	BOOL						bqueried;
} list_gameservers_s;


static int check_timeout(SOCKET sock, long timeout)
{
	struct timeval		t;
	fd_set				fd;

	t.tv_sec = timeout;
	t.tv_usec = 0;

	FD_ZERO(&fd); 
	FD_SET(sock, &fd);

	return select(0, &fd, NULL, NULL, &t);
}


static list_gameservers_s *list_gameservers_add(list_gameservers_s **p, char *ip, unsigned short port) 
{
    list_gameservers_s *n = (list_gameservers_s*)malloc(sizeof(list_gameservers_s));
    if (n == NULL)
        return NULL;
	memset(n, 0, sizeof(list_gameservers_s));
    n->next = *p;                                                                            
    *p = n;
    n->port = port;
	memset(n->ip, 0, sizeof(n->ip));
	strncpy_s(n->ip, sizeof(n->ip), ip, sizeof(n->ip) - 1);
	n->net_ip = inet_addr(ip);

    return *p;
}


DXSLLIB_API int ObtainServers(SL *handle, struct masterserver_s *masterserver, long timeout) 
{
	SOCKET				sock;
	struct sockaddr_in	server;
    int					t, len = 0;
	unsigned char		*sec = NULL;
	unsigned char		validate[43] = {0};
	unsigned long		count = 0;
	char				data[MAX_RECV_BUFF];
	char				*port;
	list_gameservers_s	*list_gs = NULL;

	if (!handle)
		return ERROR_WRONG_PARAM;

	*handle = NULL;

	if (!masterserver)
		return ERROR_WRONG_PARAM;

	if (!masterserver->hostname || !masterserver->port)
		return ERROR_WRONG_PARAM;

	if ((sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP)) < 1)
		return ERROR_SOCKET_INIT_FAILED;

	server.sin_addr.s_addr = resolve(masterserver->hostname);
    server.sin_port = htons(masterserver->port);
    server.sin_family = AF_INET;

	if (connect(sock, (struct sockaddr *)&server, sizeof(server)) < 0)
	{
		closesocket(sock);
		return ERROR_CONNECT_FAILED;
	}

    while (len < sizeof(data) && (t = check_timeout(sock, timeout)) > 0) 
	{
        t = recv(sock, data + len, sizeof(data) - len, 0);
        if (t < 1)
			break;
        len += t;
        data[len] = 0;
        sec = (unsigned char *)strstr(data, STR_SECURE);
        if (sec && (strlen((char *)sec + 8) >= 6)) 
			break;
        if (!data[len - 1]) 
			break;
    }

	if (!sec && t == 0)
	{
		closesocket(sock);
		return ERROR_SOCKET_TIMEOUT;
	}
	else if (!sec && t < 0)
	{
		closesocket(sock);
		return ERROR_SOCKET_FAILED;
	}
	else if (!sec)
	{
		closesocket(sock);
		return ERROR_VALIDATION;
	}
	else
	{
		sec += 8;
		gsseckey(validate, sec);
	}

	if (send(sock, VLD_PART1, sizeof(VLD_PART1)-1, 0) < 0)
	{
		closesocket(sock);
		return ERROR_SOCKET_FAILED;
	}

	if (send(sock, validate, (int)strlen(validate), 0) < 0)
	{
		closesocket(sock);
		return ERROR_SOCKET_FAILED;
	}

	if (send(sock, VLD_PART2, sizeof(VLD_PART2)-1, 0) < 0)
	{
		closesocket(sock);
		return ERROR_SOCKET_FAILED;
	}

	memset(data, 0, sizeof(data));
	len = 0;
    while (len < (sizeof(data) - 1) && (t = check_timeout(sock, timeout)) > 0) 
	{
        t = recv(sock, data + len, 1, 0);
        if (t < 1)
			break;
        len += t;
		sec = (unsigned char *)strstr(data, STR_IP);
		if (sec)
		{
			*sec = 0;
			port = strchr(data, ':');
			if (port != NULL)
			{
				*port = 0;
				port++;
				if (list_gameservers_add(&list_gs, data, atoi(port)) != NULL)
					count++;
			}
			memset(data, 0, sizeof(data));
			len = 0;
		}
        sec = (unsigned char *)strstr(data, STR_FINAL);
        if (sec) 
			break;
    }

	if (!sec && t == 0)
	{
		closesocket(sock);
		return ERROR_SOCKET_TIMEOUT;
	}
	else if (!sec && t < 0)
	{
		closesocket(sock);
		return ERROR_SOCKET_FAILED;
	}
	else if (sec)
	{
		*sec = 0;
		port = strchr(data, ':');
		if (port != NULL)
		{
			*port = 0;
			port++;
			if (list_gameservers_add(&list_gs, data, atoi(port)) != NULL)
				count++;
		}
	}
	else
	{
		closesocket(sock);
		return ERROR_UNKNOWN;
	}

	closesocket(sock);

	*handle = list_gs;

	return count;
}


DXSLLIB_API int GetNextServer(SL *handle, struct gameserver_s *gameserver)
{
	list_gameservers_s		*list_gs;

	if (*handle < (HANDLE)1)
		return ERROR_NO_MORE_SERVERS;

	if (!gameserver)
		return ERROR_WRONG_PARAM;

	list_gs = (list_gameservers_s*)*handle;

	gameserver->query_port = list_gs->port;
	memset(gameserver->ip, 0, sizeof(gameserver->ip));
	strncpy_s(gameserver->ip, sizeof(gameserver->ip), list_gs->ip, sizeof(gameserver->ip) - 1);

	*handle = list_gs->next;

	free(list_gs);

	return ERROR_ALL_OK;
}


DXSLLIB_API int GetNextServerInfo(SL *handle, struct gameserver_s *gameserver)
{
	list_gameservers_s		*list_gs;

	if (*handle < (HANDLE)1)
		return ERROR_NO_MORE_SERVERS;

	if (!gameserver)
		return ERROR_WRONG_PARAM;

	list_gs = (list_gameservers_s*)*handle;

	gameserver->query_port = list_gs->port;
	memset(gameserver->ip, 0, sizeof(gameserver->ip));
	strncpy_s(gameserver->ip, sizeof(gameserver->ip), list_gs->ip, sizeof(gameserver->ip) - 1);
	memcpy(&gameserver->server_data, &list_gs->server_data, sizeof(gameserver->server_data));
	gameserver->bqueried = list_gs->bqueried;

	*handle = list_gs->next;

	free(list_gs);

	return ERROR_ALL_OK;
}


static void FillServerData(list_gameservers_s *node, char *option, char *value)
{
	if (!option || !value)
		return;

	if (!strcmp(option, STR_OPTION_HOSTNAME))
		strncpy_s(node->server_data.hostname, sizeof(node->server_data.hostname), value, sizeof(node->server_data.hostname) - 1);
	else if (!strcmp(option, STR_OPTION_GAMETYPE))
		strncpy_s(node->server_data.gametype, sizeof(node->server_data.gametype), value, sizeof(node->server_data.gametype) - 1);
	else if (!strcmp(option, STR_OPTION_MAPNAME))
		strncpy_s(node->server_data.mapname, sizeof(node->server_data.mapname), value, sizeof(node->server_data.mapname) - 1);
	else if (!strcmp(option, STR_OPTION_HOSTPORT))
		node->server_data.hostport = (unsigned short)atoi(value);
}


static void ParsePacket(list_gameservers_s *node, char *data, int len)
{
	int		k = 1, i;
	char	*optval[2], *p;

	if (!data)
		return;

	node->bqueried = TRUE;

	p = data;

	optval[0] = NULL;
	optval[1] = NULL;

	for ( ; *p != 0; p++)
		if (*p == '\\')
			*p = 0;

	for (i = 0; i < len; i++, data++)
	{
		if (*data == 0)
		{
			k = k ? 0 : 1;
		}
		if (*(data-1) == 0 && i > 0)
			optval[k] = data;

		if (k)
			FillServerData(node, optval[0], optval[1]);
	}
}


static int ReceiveQueries(list_gameservers_s **list_gs, SOCKET sock, long timeout)
{
	char				buffer[MAX_RECV_BUFF];
	struct sockaddr_in  from;
	int					fromsize = sizeof(from);
	list_gameservers_s	*node;
	int					len;

	while (check_timeout(sock, timeout) > 0)
	{
		memset(buffer, 0, sizeof(buffer));
		len = recvfrom(sock, buffer, sizeof(buffer) - 1, 0, (struct sockaddr *)&from, &fromsize);

		node = *list_gs;

		while (node != NULL)
		{
			if (from.sin_addr.s_addr == node->net_ip && from.sin_port == htons(node->port))
				ParsePacket(node, buffer, len);

			node = node->next;
		}
	}

	return ERROR_ALL_OK;
}


DXSLLIB_API int QueryServerList(SL *handle, long timeout)
{
	SOCKET					sock;
	struct sockaddr_in		server;
	list_gameservers_s		*list_gs;

	if (*handle < (HANDLE)1)
		return ERROR_WRONG_PARAM;

	if ((sock = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP)) < 1)
		return ERROR_SOCKET_INIT_FAILED;

	list_gs = (list_gameservers_s*)*handle;

	do
	{
		server.sin_addr.s_addr = list_gs->net_ip;
		server.sin_port        = htons(list_gs->port);
		server.sin_family      = AF_INET;
	     
		sendto(sock, STR_STATUS, sizeof(STR_STATUS) - 1, 0, (struct sockaddr *)&server, sizeof(server));
	} while ((list_gs = list_gs->next) != NULL);

	ReceiveQueries((list_gameservers_s**)handle, sock, timeout);

	closesocket(sock);

	return ERROR_ALL_OK;	
}


BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	static WSADATA winsockdata;

	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		if (WSAStartup(MAKEWORD(2, 2), &winsockdata))
			return FALSE;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
    return TRUE;
}