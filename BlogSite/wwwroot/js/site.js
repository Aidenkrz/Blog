document.addEventListener('DOMContentLoaded', () => {
    const animationQueue = []; 
    let isAnimating = false; 
    const animationDelayBetweenElements = 100; 

    const observerOptions = {
        root: null,
        rootMargin: '0px', 
        threshold: 0.1 
    };

    function processAnimationQueue() {
        if (animationQueue.length === 0) {
            isAnimating = false;
            return; 
        }
        isAnimating = true;
        const entry = animationQueue.shift(); 
        const target = entry.target;

        if (target.classList.contains('setup-word-reveal')) {
            const words = target.querySelectorAll('span.word-span');
            words.forEach((word, index) => {
                const delay = index * 0.04; 
                word.style.transitionDelay = `${delay}s`;
            });
        }
        target.classList.add('is-visible');
        intersectionObserver.unobserve(target);
        setTimeout(processAnimationQueue, animationDelayBetweenElements);
    }

    const observerCallback = (entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                animationQueue.push(entry);
                if (!isAnimating) {
                    processAnimationQueue();
                }
            }
        });
    };

    const intersectionObserver = new IntersectionObserver(observerCallback, observerOptions);

    function wrapNodeWords(node) {
        if (node.nodeType === Node.TEXT_NODE) {
            if (node.parentNode && node.parentNode.nodeType === Node.ELEMENT_NODE && node.parentNode.classList.contains('math')) {
                return; 
            }
            const text = node.textContent || "";
            const wordsAndSpaces = text.split(/(\s+)/).filter(part => part.length > 0);
            if (wordsAndSpaces.length > 1 || (wordsAndSpaces.length === 1 && wordsAndSpaces[0].trim().length > 0)) {
                const fragment = document.createDocumentFragment();
                wordsAndSpaces.forEach(part => {
                    if (part.trim().length > 0) { 
                        const span = document.createElement('span');
                        span.className = 'word-span';
                        span.textContent = part;
                        fragment.appendChild(span);
                    } else { 
                        fragment.appendChild(document.createTextNode(part));
                    }
                });
                node.parentNode?.replaceChild(fragment, node);
            }
        } else if (node.nodeType === Node.ELEMENT_NODE) {
            if (node.nodeName !== 'SPAN' || !node.classList.contains('word-span')) {
                 const children = Array.from(node.childNodes);
                 children.forEach(wrapNodeWords);
            }
        }
    }

    const elementsToAnimate = document.querySelectorAll(
        '.blog-post-item, .blog-post-content img, .blog-post-content pre, .blog-post-content table, .blog-post-content figure, .blog-post-content ul, .blog-post-content ol, .blog-post-content blockquote, .blog-post-content p, .blog-post-content h2, .blog-post-content h3, .blog-post-content h4, .blog-post-content h5, .blog-post-content h6'
    );

    elementsToAnimate.forEach(el => {
        if (['P', 'H2', 'H3', 'H4', 'H5', 'H6'].includes(el.tagName)) {
            el.classList.add('setup-word-reveal'); 
            wrapNodeWords(el); 
        }
        intersectionObserver.observe(el);
    });

    const progressBar = document.querySelector('.scroll-progress-bar');
    if (progressBar) {
        const updateProgressBar = () => {
            const totalScroll = document.documentElement.scrollHeight - window.innerHeight;
            const currentScroll = window.scrollY;
            const scrollPercentage = totalScroll > 0 ? (currentScroll / totalScroll) * 100 : 0; 
            const widthPercentage = Math.min(scrollPercentage, 100);
            progressBar.style.width = `${widthPercentage}%`;
        };
        window.addEventListener('scroll', updateProgressBar);
        updateProgressBar(); 
    }

    document.querySelectorAll('pre code[class*="language-"]').forEach(codeBlock => {
        const preElement = codeBlock.parentElement;
        if (!preElement || preElement.tagName !== 'PRE' || preElement.querySelector('.code-language-label')) {
             return; 
        }
        const langClass = Array.from(codeBlock.classList).find(cls => cls.startsWith('language-'));
        if (langClass) {
            const language = langClass.replace('language-', '').toLowerCase();
            const label = document.createElement('div');
            label.className = 'code-language-label';
            label.textContent = language;
            preElement.insertBefore(label, preElement.firstChild);
        }
    });

    try {
        if (typeof renderMathInElement === 'function') {
            const contentArea = document.querySelector('.blog-post-content') || document.body;
            renderMathInElement(contentArea, {
                delimiters: [
                    {left: '$$', right: '$$', display: true},    
                    {left: '$', right: '$', display: false},     
                    {left: '\\(', right: '\\)', display: false}, 
                    {left: '\\[', right: '\\]', display: true}   
                ],
                throwOnError : false 
            });
            console.log("KaTeX auto-render executed with configured delimiters.");
        } else {
            console.warn("KaTeX auto-render script not loaded or renderMathInElement not defined.");
        }
    } catch (error) {
        console.error("Error during KaTeX auto-render setup/execution:", error);
    }

});

// Holy shitcode

const playIconSVG = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-play-fill" viewBox="0 0 16 16"><path d="m11.596 8.697-6.363 3.692c-.54.313-1.233-.066-1.233-.697V4.308c0-.63.692-1.01 1.233-.696l6.363 3.692a.802.802 0 0 1 0 1.393"/></svg>`;
const pauseIconSVG = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-pause-fill" viewBox="0 0 16 16"><path d="M5.5 3.5A1.5 1.5 0 0 1 7 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5m5 0A1.5 1.5 0 0 1 12 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5"/></svg>`;
 
let audioPlayer, playerWidget, albumArt, trackTitle, trackArtist, progressBarElem, currentTimeEl, totalTimeEl, playPauseButton, volumeButton, volumePanel, volumeSlider; 
let playerInitialized = false; 

function initializeAudioPlayer(trackData) {
    console.log("initializeAudioPlayer called with trackData:", trackData); 
    
    console.log("Attempting to initialize audio player..."); 
    
    audioPlayer = document.getElementById('post-audio-player');
    playerWidget = document.getElementById('audio-player-widget');
    albumArt = document.getElementById('audio-album-art');
    trackTitle = document.getElementById('audio-title');
    trackArtist = document.getElementById('audio-artist');
    progressBarElem = document.getElementById('audio-progress');
    currentTimeEl = document.getElementById('audio-current-time');
    totalTimeEl = document.getElementById('audio-total-time');
    playPauseButton = document.getElementById('audio-play-pause');
    volumeButton = document.getElementById('audio-volume-button');   
    volumePanel = document.getElementById('audio-volume-panel');     
    volumeSlider = document.getElementById('audio-volume');       

    if (!audioPlayer || !playerWidget || !albumArt || !trackTitle || !trackArtist || !progressBarElem || !currentTimeEl || !totalTimeEl || !playPauseButton || !volumeButton || !volumePanel || !volumeSlider || !trackData || !trackData.url) { 
        console.error("Audio player initialization failed: Required elements or track data missing.", { 
            audioPlayerExists: !!audioPlayer, playerWidgetExists: !!playerWidget, albumArtExists: !!albumArt,
            trackTitleExists: !!trackTitle, trackArtistExists: !!trackArtist, progressBarExists: !!progressBarElem,
            currentTimeElExists: !!currentTimeEl, totalTimeElExists: !!totalTimeEl, playPauseButtonExists: !!playPauseButton,
            volumeButtonExists: !!volumeButton, volumePanelExists: !!volumePanel, volumeSliderExists: !!volumeSlider, 
            trackDataExists: !!trackData, trackUrlExists: !!trackData?.url 
        });
        if(playerWidget) playerWidget.classList.remove('visible'); 
        return;
    }

    console.log("Initializing audio player UI with data:", trackData);

    trackTitle.textContent = trackData.title || 'Unknown Title';
    trackArtist.textContent = trackData.artist || 'Unknown Artist';
    if (trackData.artBase64 && trackData.artBase64.startsWith('data:image')) { 
        console.log(`Setting album art src (Type: ${trackData.artBase64.substring(5, trackData.artBase64.indexOf(';'))}, Length: ${trackData.artBase64.length})`); 
        albumArt.src = trackData.artBase64;
        albumArt.style.display = 'block'; 
    } else {
        console.log("No valid album art Base64 found.");
        albumArt.src = '#'; 
        albumArt.style.display = 'none'; 
    }
    
    audioPlayer.pause();
    audioPlayer.currentTime = 0;
    progressBarElem.value = 0;
    currentTimeEl.textContent = formatTime(0);
    totalTimeEl.textContent = formatTime(0);
    playPauseButton.innerHTML = playIconSVG; 
    playPauseButton.setAttribute('aria-label', 'Play');
    audioPlayer.volume = 0.3; 
    volumeSlider.value = audioPlayer.volume * 100; 

    audioPlayer.src = trackData.url;
    console.log(`Audio src set to: ${trackData.url}`);
    
    playerInitialized = false; 

    audioPlayer.load(); 
    console.log("Called audioPlayer.load()");

    playerWidget.classList.add('visible'); 
    console.log("Player widget made visible.");

    setupAudioPlayerListeners(); 
}

function formatTime(seconds) {
    const minutes = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${minutes}:${secs < 10 ? '0' : ''}${secs}`;
}

function setupAudioPlayerListeners() {
    if (!audioPlayer || !progressBarElem || !currentTimeEl || !totalTimeEl || !playPauseButton || !volumeButton || !volumePanel || !volumeSlider || playerInitialized) {
         console.log(`Skipping listener setup: playerInitialized=${playerInitialized}, audioPlayer=${!!audioPlayer}`);
         return; 
    }
    
    console.log("Setting up audio player event listeners.");

    if (isFinite(audioPlayer.duration)) {
         console.log(`Duration (${audioPlayer.duration}) available on listener setup.`);
         totalTimeEl.textContent = formatTime(audioPlayer.duration);
         progressBarElem.value = audioPlayer.currentTime ? (audioPlayer.currentTime / audioPlayer.duration) * 100 : 0;
    } else {
         console.log("Duration not available yet on listener setup.");
         totalTimeEl.textContent = "0:00"; 
         progressBarElem.value = 0;
    }
    currentTimeEl.textContent = formatTime(audioPlayer.currentTime); 
    playPauseButton.innerHTML = audioPlayer.paused ? playIconSVG : pauseIconSVG; 

    audioPlayer.addEventListener('timeupdate', () => {
        if (audioPlayer.duration) {
            const progressPercent = (audioPlayer.currentTime / audioPlayer.duration) * 100;
            progressBarElem.value = progressPercent;
            currentTimeEl.textContent = formatTime(audioPlayer.currentTime);
        }
    });

    audioPlayer.addEventListener('loadedmetadata', () => {
        console.log(`loadedmetadata event fired. Duration: ${audioPlayer.duration}`); 
        if (isFinite(audioPlayer.duration)) { 
             totalTimeEl.textContent = formatTime(audioPlayer.duration);
             if (progressBarElem.value == 0) currentTimeEl.textContent = formatTime(0);
        } else {
             console.warn("Audio duration is not finite after loadedmetadata.");
             totalTimeEl.textContent = "??:??"; 
        }
    });

    audioPlayer.addEventListener('play', () => {
        playPauseButton.innerHTML = pauseIconSVG;
        playPauseButton.setAttribute('aria-label', 'Pause');
    });
    audioPlayer.addEventListener('pause', () => {
        playPauseButton.innerHTML = playIconSVG;
        playPauseButton.setAttribute('aria-label', 'Play');
    });
     audioPlayer.addEventListener('ended', () => { 
        playPauseButton.innerHTML = playIconSVG;
        playPauseButton.setAttribute('aria-label', 'Play');
        progressBarElem.value = 0;
        audioPlayer.currentTime = 0; 
    });

    playPauseButton.addEventListener('click', () => {
        console.log("Play/Pause button clicked. Current state:", audioPlayer.paused ? "paused" : "playing"); 
        if (!audioPlayer) return; 
        if (audioPlayer.paused || audioPlayer.ended) {
            audioPlayer.play().catch(e => console.error("Error playing audio:", e));
        } else {
            audioPlayer.pause();
        }
    });

    progressBarElem.addEventListener('input', () => {
        if (!audioPlayer) return; 
        if (isFinite(audioPlayer.duration)) { 
            const seekTime = (progressBarElem.value / 100) * audioPlayer.duration;
            console.log(`Scrubbing to: ${seekTime} (Progress bar value: ${progressBarElem.value})`);
            audioPlayer.currentTime = seekTime;
        }
    });
    
    volumeButton.addEventListener('click', (event) => {
        event.stopPropagation(); 
        volumePanel.classList.toggle('visible');
        console.log("Volume panel toggled.");
    });

    function updateVolume() {
         if (!audioPlayer || !volumeSlider) return;
        const sliderValue = volumeSlider.value;
        const newVolume = parseFloat(sliderValue) / 100;
        
        console.log(`Volume slider event: Raw value='${sliderValue}', Parsed value=${newVolume}`);

        if (!isNaN(newVolume) && newVolume >= 0 && newVolume <= 1) {
            audioPlayer.volume = newVolume;
        } else {
            console.error(`Invalid volume calculated: ${newVolume}. Slider value was: ${sliderValue}`);
        }
    }

    volumeSlider.addEventListener('input', updateVolume);
    
    volumeSlider.addEventListener('change', updateVolume);

    audioPlayer.addEventListener('volumechange', () => {
        if (!volumeSlider) return;
        const currentAudioVolume = audioPlayer.volume;
        const newSliderValue = currentAudioVolume * 100;
        console.log(`Audio volumechange event: Audio Volume=${currentAudioVolume}, Setting Slider Value=${newSliderValue}`);
        volumeSlider.value = newSliderValue;
    });

    document.addEventListener('click', (event) => {
        if (playerWidget && volumePanel && volumePanel.classList.contains('visible')) {
            if (!volumeButton.contains(event.target) && !volumePanel.contains(event.target)) { 
                 volumePanel.classList.remove('visible');
                 console.log("Volume panel closed due to outside click.");
            }
        }
    });

    playerInitialized = true;
}

setTimeout(setupAudioPlayerListeners, 50);
