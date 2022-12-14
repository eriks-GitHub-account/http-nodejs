//var db = require('./database')
//var pmysql = require('promise-mysql')
//Socket IO
//import {createServer} from "http"
const {Server} = require("socket.io")

//const httpServer = createServer()

const io = new Server({
    pingInterval: 30005,		//An interval how often a ping is sent
    pingTimeout: 5000,		//The time a client has to respont to a ping before it is desired dead
    upgradeTimeout: 3000,		//The time a client has to fullfill the upgrade
    allowUpgrades: true,		//Allows upgrading Long-Polling to websockets. This is strongly recommended for connecting for WebGL builds or other browserbased stuff and true is the default.
    cookie: false,			//We do not need a persistence cookie for the demo - If you are using a load balöance, you might need it.
    serveClient: true,		//This is not required for communication with our asset but we enable it for a web based testing tool. You can leave it enabled for example to connect your webbased service to the same server (this hosts a js file).
    allowEIO3: false,			//This is only for testing purpose. We do make sure, that we do not accidentially work with compat mode.
    cors: {
        origin: "*"				//Allow connection from any referrer (most likely this is what you will want for game clients - for WebGL the domain of your sebsite MIGHT also work)
    }
})

//httpServer.listen(process.env.PORT || 5000)

/*
var io = require('socket.io')(5000, {

    pingInterval: 30005,		//An interval how often a ping is sent
    pingTimeout: 5000,		//The time a client has to respont to a ping before it is desired dead
    upgradeTimeout: 3000,		//The time a client has to fullfill the upgrade
    allowUpgrades: true,		//Allows upgrading Long-Polling to websockets. This is strongly recommended for connecting for WebGL builds or other browserbased stuff and true is the default.
    cookie: false,			//We do not need a persistence cookie for the demo - If you are using a load balöance, you might need it.
    serveClient: true,		//This is not required for communication with our asset but we enable it for a web based testing tool. You can leave it enabled for example to connect your webbased service to the same server (this hosts a js file).
    allowEIO3: false,			//This is only for testing purpose. We do make sure, that we do not accidentially work with compat mode.
    cors: {
        origin: "*"				//Allow connection from any referrer (most likely this is what you will want for game clients - for WebGL the domain of your sebsite MIGHT also work)
    }
})
*/
/*
app.get('/', (req, res) => {
    res.sendFile(__dirname + '/index.html');
});
*/
var startingGames = []
var startingStatusTimer = 10000
var playerList = {}

var roomData = {}

var server = {}

// JavaScript objecti yksittäisestä pelaajasta. 
// Players taulukko on täynnä näitä player objekteja.
var player = {}


console.log('Starting Socket.IO server')
//console.log(Object.keys(buildings).lenght + " taloja")
// 'connection' on event, joka emittoidaan Unityn puolella
io.on('connection', (socket) => {
    console.log('[' + (new Date()).toUTCString() + '] unity connecting with SocketID ' + socket.id)


////////////////////////////////////////////////////////////////////////////////////////

    socket.on('JOIN', async (data) => {
        var joined = false
        const rooms = io.of("/").adapter.rooms;
        //Luodaan pelaaja
        player = {
            name: data.name,
            playerID: data.playerID,
            selectedLevel: data.selectedLevel,
            roomName: "",
            playerSocketID: socket.id,
            // SKINS
            headID: data.headID,
            torsoID: data.torsoID,
            handsID: data.handsID,
            legsID: data.legsID,
            feetID: data.feetID,
            weaponSkinID: data.weaponSkinID,
            weaponElementID: data.weaponElementID,
        }
        if(startingGames.length > 0){
            startingGames.forEach(roomName => {
                if(roomName.startsWith(player.selectedLevel + 'Level')){
                    // Check if player is already in the game
                    var inGame = false
                    playerList[roomName].forEach(player => {
                        if (player.playerSocketID == socket.id){
                            inGame = true
                            return
                        }
                    })
                    if (!inGame){ // If the player is not in the game
                        joined = true
                        socket.join(roomName)
                        console.log(player.name + " joined: " + roomName)
                        player.roomName = roomName
                        playerList[roomName].push(player)
                        io.to(roomName).emit("DEBUG_MESSAGE", "MESSAGE FROM SERVER: " + player.name + " joinas huoneeseen " + newRoomName)
                        return
                    }
                }
            });
        }
        if(!joined){
            var newRoomName = player.selectedLevel + 'Level' + Math.floor(Math.random() * 1000)
            playerList[newRoomName] = []
            player.roomName = newRoomName
            playerList[newRoomName].push(player)
            socket.join(newRoomName)
            startingGames.push(newRoomName)
            console.log(startingGames)
            // Odotellaan hetki, että muut pelaajat kerkeävät joinii
            await waitFor(startingStatusTimer)
            startingGames.pop(newRoomName)
            io.to(newRoomName).emit("START_GAME", JSON.stringify(playerList[newRoomName]))
            roomData[newRoomName] = {   
                attackPackages: [],
                directions: [],
                damageList: [],
                readyToStartAttack: 0,
                waitingNextRound: 0,          
            }

            console.log(startingGames)
            console.log(playerList[newRoomName])
            io.to(newRoomName).emit("DEBUG_MESSAGE", "MESSAGE FROM SERVER: " + player.name + " loi huoneen " + newRoomName)
        }
    })

    /* DEPRECATED
    socket.on('SEND_DAMAGE', (data) =>{
        console.log("SEND_DAMAGE. Damage amount:" + data.damage)
        roomData[data.roomName].damageList.push(data)
    })
*/
    socket.on('SEND_GHOST_BOOST', (data) =>{ // BOOST, PLAYER ID, ROOM ID
        console.log("SEND_GHOST_BOOST")
        for(i = 0; i < roomData[data.roomName].attackPackages; i++){
            if(roomData[data.roomName].attackPackages[i].playerID == data.playerID){
                roomData[data.roomName].attackPackages[i].ghostBoosts += 1;
            }
        }
    })

    socket.on('SEND_EMOTE', (data) =>{
        console.log("SEND_EMOTE: " + data)
        io.to(data.roomName).emit("GET_EMOTE", data)
    })

    socket.on('CHOOSE_DIRECTION', (data) =>{
        console.log("CHOOSE_DIRECTION called on room: " + data.roomName)
        roomData[data.roomName].attackPackages.push(data)        

        //if(roomData[data.roomName].attackPackages.length == io.sockets.clients(data.roomName).length){
        if(roomData[data.roomName].attackPackages.length == playerList[data.roomName].length){
            io.to(data.roomName).emit('START_ATTACK', JSON.stringify(roomData[data.roomName].attackPackages))
            console.log("Emitted START_ATTACK")
        }
    })

    socket.on('WAITING_NEXT_ROUND', (roomName) => {
        console.log("WAITING_NEXT_ROUND")
        roomData[roomName].waitingNextRound += 1
        if(roomData[roomName].waitingNextRound == playerList[roomName].length){
            StartRound(roomName)
        }
    })


////////////////////////////////////////////////////////////////////////////////////////


    socket.on('disconnect', (reason) => {
        console.log('[' + (new Date()).toUTCString() + '] ' + socket.id + 'disconnected: Reason ' + reason);

    })

})

const waitFor = (time) => {
    return new Promise((resolve, reject) => {
      setTimeout(() => resolve(true), time);
    });
};

function StartRound(roomName){
    roomData[roomName].waitingNextRound = 0
    roomData[roomName].attackPackages = []
    bossDirection = Math.random()
    if (bossDirection < 0.5){
        bossDirection = 0
    }else bossDirection = 1
    io.to(roomName).emit('START_ROUND', bossDirection)

}

io.listen(process.env.PORT || 80)