async function api(pfad, optionen) {
    const r = await fetch(pfad, optionen);
    if (r.status === 401) {
        window.location.href = "/api/login";
        throw new Error("nicht angemeldet");
    }
    return r;
}

async function LoadList() {

    //get list from server
    const response = await api("/api/artikel");
    const data = await response.json();
    
    const meldung = document.getElementById("meldung");
    

    //error if response.ok == false
    if (!response.ok) {
        meldung.textContent = `Error: ${response.status}`
        return;
    }

    //get list from html set to empty
    GenTable(data);

}

//add this to head after testing/styling <script src="https://cdn.tailwindcss.com"></script>

async function LoadStats() {
    //get stats from api
    const response = await api("/api/statistik");
    const data = await response.json();

    //make html elements to js objects
    const statc = document.getElementById("statc"); //anzahl unerledigter Einträge
    const statlist = document.getElementById("statlist"); //Liste unerledigter Einträge
    const statsum = document.getElementById("statsum"); //Summe der Preise unerledigter Einträge

    statc.textContent = "Anzahl offener Artikel: " + data.count;
    statsum.textContent = "Summe offener Artikel: " + data.sum + "€";
    if (data.list != null) {
        statlist.textContent = "";
        for (const item of data.list) {
            const li = document.createElement("li");
            li.textContent = `${item.name}: ${item.price}€ `;
            statlist.appendChild(li);
        }
    }
    else {
        statlist.textContent = "keine Artikel in der Liste";
        statsum.textContent = "Summe: 0€";
        statc.textContent = "Anzahl: 0";
    }
}

async function loadUser() {
    const r = await api("/api/me");
    if (!r.ok) return;
    const me = await r.json();
    document.getElementById("benutzerName").textContent = "logged in as: ${me.name}" ?? "";
}

async function Logout() {
    window.location.href = "/api/logout"
}

//schickt name und preis eines neuen Artikels an den Server
async function add() {
    const name = document.getElementById("name");
    const preis = document.getElementById("preis");
    const meldung = document.getElementById("meldung");

    if (name.value == null || name.value == "" || preis.value <= 0) {
        meldung.textContent = "Falsche Eingabe, bitte befülle beide Felder";
        return;
    }

    const post = await api("/api/artikel", {
        method: "POST", 
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({name: name.value, price: preis.value})
    });

    (post.ok) ? meldung.textContent = "Artikel erfolgreich angelegt" : textContent = "Fehler beim anlegen des Artikels";
    await LoadPage();
}

//send put to server to set bool done of an item
async function SetDone(id,erl) {
    const put = await api(`/api/artikel/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({erl})
    });
    (put.ok) ? meldung.textContent = "Status erfolgreich gesetzt" : meldung.textContent = "Fehler beim setzen des Status";
    await LoadPage();
}
//TODO implement that you can set the status of a task at will instead of only to true
//function for setdone to set done status for every checked listelement
async function HandleDoneList() {
    
    const list = document.getElementById("table");
    const boxen = list.querySelectorAll("input[type='checkbox']:checked"); 
    for (const cb of boxen)
    {
        var erl = true;
        const tr = cb.closest("tr");
        if (tr.cells[3].textContent === "erledigt") erl = false;
        else erl = true;
    /*    erl = (cb) => {
            const tr = cb.closest("tr");
            (tr.cells[3].textContent === "erledigt") ? true : false;
        }*/
        await SetDone(Number(cb.dataset.id), erl);
    }

}

//Remove and Done handler changed elementid to table for migration

//send remove call to server to remove item with specified id
async function Remove(id) {
    return await api(`/api/artikel/${id}`, {
        method: "DELETE"
    });
}

//function to remove every checked element/item from list
async function HandleRemoveList() {
    const list = document.getElementById("table");
    
    const boxen = list.querySelectorAll("input[type='checkbox']:checked");

    for (const cb of boxen) {
        
          await Remove(Number(cb.dataset.id));
    }
    
    await LoadPage();
}

//loads list and stats for the page
//maybe additional functionality
async function LoadPage() {
    await LoadList();
    await LoadStats();
    await loadUser();
}

//html shenanigans
async function GenTable(data)
{
    const tbb = document.getElementById("atbody");
    tbb.replaceChildren();
  
    for (const item of data) {
        const tr = document.createElement("tr");
        const td1 = document.createElement("td");
        const td2 = document.createElement("td");
        const td3 = document.createElement("td");
        const td4 = document.createElement("td");
        const ip = document.createElement("input");

        ip.type = "checkbox";
        ip.classList.add("h-5","w-5","accent-neutral-800");
        //ip.classList.add("appearance-none", "h-4", "w-4", "rounded", "border", "bg-neutral-100", "cursor-pointer", "checked:bg-neutral-600");
        ip.dataset.id = item.id;
        
        
        td2.textContent = item.name;
        td3.textContent = `${item.price}€`;
        //td4.textContent = (item.done) => "erledigt" : "offen";
        if (item.done) { td4.textContent = "erledigt"; tr.classList.add("bg-green-100") }
        else { td4.textContent = "offen"; tr.classList.add("bg-red-100")}

        td1.appendChild(ip);
        tbb.appendChild(tr);
        tr.appendChild(td1);
        tr.appendChild(td2);
        tr.appendChild(td3);
        tr.appendChild(td4);

    }

    
    

}

