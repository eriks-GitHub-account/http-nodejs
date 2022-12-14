var express = require('express');
var jsonParser = express.json();
var urlEndodedParser = express.urlencoded({extended:false});


module.exports = function(app, mysql){
    
    app.get('/api/person/:id', function(req,res){
        // Hae informaatio tietokannasta, tässä tapuksesa henkilö, jonka id:n arvo = id
        // Voidan esimerkiksi palauttaa data jsonina clientille osana responsea. 


        // Tehdään yhteys annetuilla parametreilla
        var con = mysql.createConnection({

            host: "localhost",
            user: "root",
            password: "",
            database: "node_backend"
        });
        con.connect();
        
        // Tehdään haku kannasta
        con.query('SELECT id, firstname, lastname FROM person WHERE id='+req.params.id, 
            function(err, rows){
                
                if(err) throw err;
                console.log("Haetaan dataa tietokannasta");
                console.log(rows[0].firstname);
                // Palautetana JSONina vaikka Unityyn
                res.json({firstname: rows[0].firstname, lastname:rows[0].lastname});
            }
        );
        // Suljetaan tietokantayhteys
        con.end();
    });

 

    app.post('/api/person', urlEndodedParser ,function(req,res){


            // Data tulee tänne wwwform muodossa, joka voidaan laittaa tietokantaan. 
            console.log("Unityssa painettiin k-kirjainta. Tämä ajetaan. ");

            // Tässä tapahtuu tietokantaan laitto. 
            var con = mysql.createConnection({

                host: "localhost",
                user: "root",
                password: "",
                database: "node_backend"
            });
            con.connect();

            var sql = "INSERT INTO person (firstname, lastname) VALUES ('"+req.body.FirstName+"','"+req.body.LastName+"')";

            con.query(sql, function(err, result){
                if(err) throw err;
                console.log("Lisättiin uusi rivi tauluun peron");
            });
            con.end();
            // Systeemi olettaa, että kun tehdään request, niin palvelin myös tekee responsen, eli palautetaan
            // jotain takaisin Unityyn. Onhan se kohteliasta tehdä jotakin pyynnölle. t. Aspa. 
            // Kun tieto on laitettu tietokantaan, voidaan palauttaa jotain responsena takaisin unityyn. 
            res.json({firstname: req.body.FirstName, lastname: req.body.LastName});
    });

    app.delete('/api/person/:id', function(req,res){
        // Tännekin tieto voi tulla jostain json muodossa. 
        // POISTA informaatio eli henkilö tietokannasta. Tässä tapuksessa henkilö jonka id:n arvo on id. 

    });
}