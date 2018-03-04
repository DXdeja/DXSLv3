
#ifdef DXSLLIB_EXPORTS
#define DXSLLIB_API __declspec(dllexport)
#else
#define DXSLLIB_API __declspec(dllimport)
#endif

#define ERROR_ALL_OK				1
#define ERROR_WRONG_PARAM			-1
#define ERROR_SOCKET_INIT_FAILED	-2
#define ERROR_CONNECT_FAILED		-3
#define ERROR_SOCKET_TIMEOUT		-4
#define ERROR_SOCKET_FAILED			-5
#define ERROR_VALIDATION			-6
#define ERROR_NO_MORE_SERVERS		-7
#define ERROR_UNKNOWN				-8

#define OPTION_HOSTNAME_MAX_SIZE	128
#define OPTION_MAPNAME_MAX_SIZE		64
#define OPTION_GAMETYPE_MAX_SIZE	64

typedef HANDLE	SL;

struct server_data_s
{
	char			hostname[OPTION_HOSTNAME_MAX_SIZE];
	unsigned short	hostport;
	char			mapname[OPTION_MAPNAME_MAX_SIZE];
	char			gametype[OPTION_GAMETYPE_MAX_SIZE];
};

struct gameserver_s
{
	char					ip[16];
	unsigned short			query_port;
	struct server_data_s	server_data;
	BOOL					bqueried;
};

struct masterserver_s
{
	char			*hostname;
	unsigned short	port;
};

DXSLLIB_API int ObtainServers(SL *handle, struct masterserver_s *masterserver, long timeout);
DXSLLIB_API int GetNextServer(SL *handle, struct gameserver_s *gameserver);
DXSLLIB_API int GetNextServerInfo(SL *handle, struct gameserver_s *gameserver);
DXSLLIB_API int QueryServerList(HANDLE *handle, long timeout);