const documentInput = document.getElementById("document-input");
const generateBtn   = document.getElementById("generate-btn");
const statusEl      = document.getElementById("status");
const cardsList     = document.getElementById("cards-list");
const currentCard   = document.getElementById("current-card");
const cardFront     = document.getElementById("card-front");
const cardBack      = document.getElementById("card-back");
const flipBtn       = document.getElementById("flip-btn");
const nextBtn       = document.getElementById("next-btn");

let flashcards = [];
let currentCardIndex = -1;

// Toggle flip
flipBtn.addEventListener("click", () => {
  currentCard.classList.toggle("flipped");
});

// Next card
nextBtn.addEventListener("click", () => {
  if (flashcards.length === 0) {
    cardFront.textContent = "No cards yet";
    cardBack.textContent = "Generate flashcards first.";
    currentCard.classList.remove("flipped");
    return;
  }
  currentCardIndex = (currentCardIndex + 1) % flashcards.length;
  const card = flashcards[currentCardIndex];
  cardFront.textContent = card.question;
  cardBack.textContent  = card.answer;
  currentCard.classList.remove("flipped");
});

// Upload document → generate flashcards
generateBtn.addEventListener("click", async () => {
  const file = documentInput.files[0];
  if (!file) {
    statusEl.textContent = "Please select a PDF.";
    return;
  }

  statusEl.textContent = "Uploading document and generating flashcards...";
  flashcards = [];
  cardsList.innerHTML = "";

  const formData = new FormData();
  formData.append("document", file);

  try {
    const res = await fetch("http://localhost:5027/api/flashcards", {
      method: "POST",
      body: formData
    });

    if (!res.ok) {
      const text = await res.text();
      statusEl.textContent = `Error: ${text}`;
      console.error("API error:", text);
      return;
    }

    const cards = await res.json();
    flashcards = cards;
    renderCards();
    statusEl.textContent = `Generated ${cards.length} flashcards.`;

    if (cards.length > 0) {
      currentCardIndex = 0;
      cardFront.textContent = cards[0].question;
      cardBack.textContent  = cards[0].answer;
      currentCard.classList.remove("flipped");
    }
  } catch (err) {
    statusEl.textContent = `Network error: ${err.message}`;
    console.error("Network error:", err);
  }
});

function renderCards() {
  cardsList.innerHTML = "";
  flashcards.forEach(card => {
    const div = document.createElement("div");
    div.className = "card-item";
    div.innerHTML = `
      <p><strong>Q:</strong> ${card.question}</p>
      <p><strong>A:</strong> ${card.answer}</p>
    `;
    cardsList.appendChild(div);
  });
}